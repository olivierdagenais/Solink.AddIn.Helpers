using System;
using System.IO;
using NDesk.Options;
using Solink.AddIn.Helpers;

namespace Solink.AddIn.GenerateRestartableAddIn
{
    class Program
    {
        static void Main(string[] args)
        {
            var generator = CreateGeneratorFromArguments(args);
            generator.Generate();
        }

        internal static RestartableAddInGenerator CreateGeneratorFromArguments(string[] args)
        {
            string namespaceName = null, sourceAssembly = null, targetFolder = null;
            var optionSet = new OptionSet
            {
                {"namespace=", v => namespaceName = v},
                {"sourceAssembly=", v => sourceAssembly = v},
                {"targetFolder=", v => targetFolder = v},
            };
            optionSet.Parse(args);
            if (String.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException("'namespace' must be provided.");
            }
            if (String.IsNullOrEmpty(sourceAssembly))
            {
                throw new ArgumentException("'sourceAssembly' must be provided.");
            }
            var sourceAssemblyFileInfo = new FileInfo(sourceAssembly);
            if (!sourceAssemblyFileInfo.Exists)
            {
                throw new ArgumentException("The 'sourceAssembly' specified by '{0}' could not be found.");
            }
            if (String.IsNullOrEmpty(targetFolder))
            {
                throw new ArgumentException("'targetFolder' must be provided.");
            }
            var targetFolderInfo = new DirectoryInfo(targetFolder);
            targetFolderInfo.Create();

            var result = new RestartableAddInGenerator(namespaceName, sourceAssemblyFileInfo, targetFolderInfo);
            return result;
        }
    }
}
