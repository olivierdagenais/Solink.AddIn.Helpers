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
    }
}
