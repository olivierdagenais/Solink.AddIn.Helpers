using System;
using System.AddIn.Hosting;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Solink.AddIn.Helpers
{
    public class RestartableAddInGenerator
    {
        internal const string BaseActionMethodName = "Action";
        internal const string BaseFuncMethodName = "Func";
        internal const string ClassNamePrefix = "Restartable";
        internal const string GeneratedClassNameKey = "GeneratedClassName";

        private static readonly string RestartableAddInTypeName = typeof (RestartableAddIn<>).FullName;

        private readonly string _namespaceName;
        private readonly FileInfo _sourceAssembly;
        private readonly DirectoryInfo _targetFolder;
        private readonly CodeDomProvider _provider = CodeDomProvider.CreateProvider("CSharp");
        private readonly CodeGeneratorOptions _options = new CodeGeneratorOptions
        {
            BracingStyle = "C",
        };

        public RestartableAddInGenerator(string namespaceName, FileInfo sourceAssembly, DirectoryInfo targetFolder)
        {
            _namespaceName = namespaceName;
            _sourceAssembly = sourceAssembly;
            _targetFolder = targetFolder;
        }

        public void Generate()
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(_sourceAssembly.FullName);
            Generate(assembly);
        }

        internal void Generate(Assembly sourceAssembly)
        {
            var exportedTypes = sourceAssembly.GetExportedTypes();
            var exportedInterfaces = exportedTypes.Where(t => t.IsInterface);
            foreach (var interfaceType in exportedInterfaces)
            {
               Generate(interfaceType); 
            }
        }

        internal void Generate(Type type)
        {
            var compileUnit = GenerateFromType(_namespaceName, type);

            var generatedClassName = (string)compileUnit.UserData[GeneratedClassNameKey];
            var fileNameExt = Path.ChangeExtension(generatedClassName, ".cs");
            var path = Path.Combine(_targetFolder.FullName, fileNameExt);

            using(var s = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var sw = new StreamWriter(s, Encoding.UTF8))
            {
                _provider.GenerateCodeFromCompileUnit(compileUnit, sw, _options);
            }
        }

        internal static CodeCompileUnit GenerateFromType(string namespaceName, Type type)
        {
            var className = GenerateClassName(type.Name);
            var hostViewFullName = type.FullName;
            var result = CreateCompileUnit();
            result.UserData[GeneratedClassNameKey] = className;

            var ns = CreateNamespace(result, namespaceName);
            var @class = CreateClass(ns, className, hostViewFullName);
            CreateConstructor(@class);
            CreateFactoryMethod(@class, hostViewFullName);

            foreach (var memberInfo in type.GetMembers())
            {
                WrapMember(@class, memberInfo);
            }

            return result;
        }

        internal static void WrapMember(CodeTypeDeclaration @class, MemberInfo memberInfo)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Method:
                    var methodInfo = (MethodInfo)memberInfo;
                    // This excludes get_* and set_* methods generated from properties
                    if (!methodInfo.Attributes.HasFlag(MethodAttributes.SpecialName))
                    {
                        WrapMethod(@class, methodInfo);
                    }
                    break;
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo) memberInfo;
                    WrapProperty(@class, propertyInfo);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("memberInfo", memberInfo.MemberType, "The interface contains a member of a type that is not supported.");
            }
        }

        internal static CodeMemberProperty WrapProperty(CodeTypeDeclaration @class, PropertyInfo propertyInfo)
        {
            var propertyName = propertyInfo.Name;
            var propertyType = propertyInfo.PropertyType;
            var includeSet = propertyInfo.CanWrite;

            var result = CreateProperty(@class, propertyName, propertyType, includeSet);
            return result;
        }

        internal static CodeMemberMethod WrapMethod(CodeTypeDeclaration @class, MethodInfo methodInfo)
        {
            var methodName = methodInfo.Name;
            var parameterInfos = methodInfo.GetParameters();
            var methodParameters = parameterInfos.Select(ConvertToTuple);
            var returnType = methodInfo.ReturnType;

            var result = CreateFuncMethod(@class, methodName, methodParameters, returnType);
            return result;
        }

        internal static Tuple<Type, string> ConvertToTuple(ParameterInfo pi)
        {
            return Tuple.Create(pi.ParameterType, pi.Name);
        }

        internal static CodeCompileUnit CreateCompileUnit()
        {
            var result = new CodeCompileUnit();
            return result;
        }

        internal static CodeNamespace CreateNamespace(CodeCompileUnit compileUnit, string namespaceName)
        {
            var result = new CodeNamespace(namespaceName);
            compileUnit.Namespaces.Add(result);
            return result;
        }

        internal static CodeTypeDeclaration CreateClass(CodeNamespace @namespace, string className, string hostViewFullName)
        {
            var result = new CodeTypeDeclaration(className)
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed,
            };
            var hostViewType = new CodeTypeReference(hostViewFullName);
            // subclasses "RestartableAddIn<IThing>"
            result.BaseTypes.Add(new CodeTypeReference(RestartableAddInTypeName, hostViewType));
            // implements "IThing"
            result.BaseTypes.Add(hostViewType);

            @namespace.Types.Add(result);
            return result;
        }

        internal static CodeConstructor CreateConstructor(CodeTypeDeclaration @class)
        {
            var result = new CodeConstructor
            {
                Attributes = MemberAttributes.Public,
            };
            var parameters = AddConstructorParameters(result);

            result.BaseConstructorArgs.AddRange(parameters);

            @class.Members.Add(result);
            return result;
        }

        internal static CodeExpression[] AddConstructorParameters(CodeMemberMethod method)
        {
            var list = new List<CodeExpression>
            {
                CreateMethodParameter(method, typeof (AddInFacade), "facade"),
                CreateMethodParameter(method, typeof (AddInToken), "token"),
                CreateMethodParameter(method, typeof (Platform), "platform"),
            };

            return list.ToArray();
        }

        internal static CodeArgumentReferenceExpression CreateMethodParameter(CodeMemberMethod method, Type type, string name)
        {
            var parameter = new CodeParameterDeclarationExpression(type, name);

            method.Parameters.Add(parameter);

            var result = new CodeArgumentReferenceExpression(name);
            return result;
        }

        internal static CodeMemberMethod CreateFactoryMethod(CodeTypeDeclaration @class, string hostViewFullName)
        {
            var result = new CodeMemberMethod
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Name = "Factory",
                ReturnType = new CodeTypeReference(hostViewFullName),
            };
            var parameters = AddConstructorParameters(result);

            var rs = new CodeMethodReturnStatement
            {
                Expression = new CodeObjectCreateExpression(
                    new CodeTypeReference(@class.Name),
                    parameters
                ),
            };
            result.Statements.Add(rs);

            @class.Members.Add(result);
            return result;
        }

        internal static CodeMemberMethod CreateFuncMethod(CodeTypeDeclaration @class, string methodName, IEnumerable<Tuple<Type, String>> methodParameters, Type returnType)
        {
            var result = new CodeMemberMethod
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = methodName,
            };
            var isFunction = returnType != null && returnType != typeof(void);
            if (isFunction)
            {
                result.ReturnType = new CodeTypeReference(returnType);
            }

            // This is to enumerate methodParameters twice
            var parameters = methodParameters as IList<Tuple<Type, string>>
                ?? methodParameters.ToList();
            foreach (var tuple in parameters)
            {
                var parameterType = tuple.Item1;
                var parameterName = tuple.Item2;
                CreateMethodParameter(result, parameterType, parameterName);
            }
            var lambdaExpression = GenerateLambdaMethodCallExpression(methodName, parameters);
            var methodExpression = 
                new CodeMethodInvokeExpression(
                    new CodeBaseReferenceExpression(),
                    isFunction ? BaseFuncMethodName : BaseActionMethodName,
                    lambdaExpression
            );
            CodeStatement methodStatement;
            if (isFunction)
            {
                methodStatement = new CodeMethodReturnStatement(methodExpression);
            }
            else
            {
                methodStatement = new CodeExpressionStatement(methodExpression);
            }
            result.Statements.Add(methodStatement);

            @class.Members.Add(result);
            return result;
        }

        /// <summary>
        /// Generate something like:
        /// <code>_ => _.methodName(methodParameters)</code>
        /// using a snippet, because CodeDOM doesn't seem support this directly.
        /// </summary>
        internal static CodeExpression GenerateLambdaMethodCallExpression(string methodName, IList<Tuple<Type, string>> parameters)
        {
            var sb = new StringBuilder();
            sb.Append("_ => _.").Append(methodName);
            sb.Append("(");
            sb.Append(String.Join(", ", parameters.Select(tuple => tuple.Item2)));
            sb.Append(")");
            return new CodeSnippetExpression(sb.ToString());
        }

        /// <summary>
        /// Generate something like:
        /// <code>_ => _.propertyName</code>
        /// using a snippet, because CodeDOM doesn't seem support this directly.
        /// </summary>
        internal static CodeExpression GenerateLambdaPropertyGetExpression(string propertyName)
        {
            var sb = new StringBuilder();
            sb.Append("_ => _.").Append(propertyName);
            return new CodeSnippetExpression(sb.ToString());
        }

        /// <summary>
        /// Generate something like:
        /// <code>_ => _.propertyName = value</code>
        /// using a snippet, because CodeDOM doesn't seem support this directly.
        /// </summary>
        internal static CodeExpression GenerateLambdaPropertySetExpression(string propertyName)
        {
            var sb = new StringBuilder();
            sb.Append("_ => _.").Append(propertyName).Append(" = value");
            return new CodeSnippetExpression(sb.ToString());
        }

        internal static CodeMemberProperty CreateProperty(CodeTypeDeclaration @class, string propertyName, Type propertyType, bool includeSet)
        {
            var result = new CodeMemberProperty
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = propertyName,
                Type = new CodeTypeReference(propertyType),
            };

            var lambdaGetExpression = GenerateLambdaPropertyGetExpression(propertyName);
            var getStatements = new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(
                    new CodeBaseReferenceExpression(),
                    BaseFuncMethodName,
                    lambdaGetExpression
                )
            );
            result.GetStatements.Add(getStatements);

            if (includeSet)
            {
                var lambdaSetExpression = GenerateLambdaPropertySetExpression(propertyName);
                var setStatements = new CodeMethodInvokeExpression(
                    new CodeBaseReferenceExpression(),
                    BaseActionMethodName,
                    lambdaSetExpression
                );
                result.SetStatements.Add(setStatements);
            }

            @class.Members.Add(result);
            return result;
        }

        internal static string GenerateClassName(string interfaceName)
        {
            if (!interfaceName.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("interfaceName does not start with 'I'", "interfaceName");
            }
            var restOfInterfaceName = interfaceName.Substring(1);
            var sb = new StringBuilder(ClassNamePrefix.Length + restOfInterfaceName.Length);
            sb.Append(ClassNamePrefix).Append(restOfInterfaceName);
            var result = sb.ToString();
            return result;
        }
    }
}
