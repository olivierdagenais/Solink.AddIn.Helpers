using System;
using System.AddIn.Hosting;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Solink.AddIn.Helpers
{
    public class RestartableAddInGenerator
    {
        private static readonly string RestartableAddInTypeName = typeof (RestartableAddIn<>).FullName;

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

        internal static CodeMemberMethod CreateActionMethod(CodeTypeDeclaration @class, string methodName, IEnumerable<Tuple<Type, String>> methodParameters)
        {
            var result = new CodeMemberMethod
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = methodName,
            };

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
            var invokeAction = new CodeMethodInvokeExpression(
                new CodeBaseReferenceExpression(),
                "Action",
                lambdaExpression
            );
            result.Statements.Add(invokeAction);

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
                ReturnType = new CodeTypeReference(returnType),
            };

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
            var invokeAction = new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(
                    new CodeBaseReferenceExpression(),
                    "Func",
                    lambdaExpression
                )
            );
            result.Statements.Add(invokeAction);

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
                    "Func",
                    lambdaGetExpression
                )
            );
            result.GetStatements.Add(getStatements);

            if (includeSet)
            {
                var lambdaSetExpression = GenerateLambdaPropertySetExpression(propertyName);
                var setStatements = new CodeMethodInvokeExpression(
                    new CodeBaseReferenceExpression(),
                    "Action",
                    lambdaSetExpression
                );
                result.SetStatements.Add(setStatements);
            }

            @class.Members.Add(result);
            return result;
        }
    }
}
