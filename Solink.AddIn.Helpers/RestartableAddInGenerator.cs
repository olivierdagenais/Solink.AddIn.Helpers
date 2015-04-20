using System;
using System.AddIn.Hosting;
using System.CodeDom;
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
            CreateMethodParameter(result, typeof (AddInFacade), "facade");
            CreateMethodParameter(result, typeof (AddInToken), "token");
            CreateMethodParameter(result, typeof (Platform), "platform");

            result.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("facade"));
            result.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("token"));
            result.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("platform"));

            @class.Members.Add(result);
            return result;
        }

        internal static CodeParameterDeclarationExpression CreateMethodParameter(CodeMemberMethod method, Type type, string name)
        {
            var result = new CodeParameterDeclarationExpression(type, name);

            method.Parameters.Add(result);
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
            CreateMethodParameter(result, typeof (AddInFacade), "facade");
            CreateMethodParameter(result, typeof (AddInToken), "token");
            CreateMethodParameter(result, typeof (Platform), "platform");

            var rs = new CodeMethodReturnStatement
            {
                Expression = new CodeObjectCreateExpression(
                    new CodeTypeReference(@class.Name),
                    new CodeArgumentReferenceExpression("facade"),
                    new CodeArgumentReferenceExpression("token"),
                    new CodeArgumentReferenceExpression("platform")
                ),
            };
            result.Statements.Add(rs);

            @class.Members.Add(result);
            return result;
        }
    }
}
