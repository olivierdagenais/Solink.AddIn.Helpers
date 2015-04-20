using System;
using System.AddIn.Hosting;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;

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
    }
}
