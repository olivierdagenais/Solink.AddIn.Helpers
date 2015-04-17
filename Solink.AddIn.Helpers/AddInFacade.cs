using System;
using System.AddIn.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using log4net;

namespace Solink.AddIn.Helpers
{
    public sealed class AddInFacade : IDisposable
    {
        private const string PlatformQualificationDataKey = "Platform";
        private static readonly ILog Log = LogManager.GetLogger(typeof (AddInFacade));
        private static readonly Assembly OurAssembly = Assembly.GetExecutingAssembly();
        private static readonly string OurAssemblyFolder = Path.GetDirectoryName(OurAssembly.Location);

        private static readonly DirectoryInfo OurAssemblyPipelineFolder =
            new DirectoryInfo(Path.Combine(OurAssemblyFolder, "Pipeline"));

        private readonly DirectoryInfo _pipelineFolder;
        private readonly IList<AddInProcess> _addInProcesses;

        public AddInFacade(DirectoryInfo pipelineFolder)
        {
            _pipelineFolder = pipelineFolder;
            if (!pipelineFolder.Exists)
            {
                const string template = "The specified pipeline folder '{0}' does not exist.";
                var message = String.Format(template, pipelineFolder.FullName);
                Log.Error(message);
                throw new ArgumentException(message);
            }

            var warnings = AddInStore.Rebuild(pipelineFolder.FullName);
            if (warnings.Length > 0)
            {
                const string template = "There were {0} warnings rebuilding the Add-In Store.";
                var message = String.Format(template, warnings.Length);
                Log.Warn(message);
                foreach (var warning in warnings)
                {
                    Log.Warn(warning);
                }
            }
            _addInProcesses = new List<AddInProcess>();
        }

        public AddInFacade() : this(OurAssemblyPipelineFolder) { /* Empty on purpose */ }

        public IList<T> ActivateAddIns<T>(Func<AddInFacade, AddInToken, Platform, T> factory)
        {
            return ActivateAddIns(factory, null);
        }

        public IList<T> ActivateAddIns<T>(Func<AddInFacade, AddInToken, Platform, T> factory, Func<AddInToken, bool> predicate)
        {
            var result = new List<T>();
            var typeOfT = typeof (T);
            var tokens = AddInStore.FindAddIns(typeOfT, _pipelineFolder.FullName);
            if (tokens.Count == 0)
            {
                const string template = "No Add-Ins of type '{0}' were found!";
                var message = String.Format(template, typeOfT.Name);
                Log.Warn(message);
            }
            foreach (var token in tokens)
            {
                if (predicate == null || predicate(token))
                {
                    var addInQualificationData = token.QualificationData[AddInSegmentType.AddIn];
                    var addInProcessPlatform = Platform.Host;
                    if (addInQualificationData.ContainsKey(PlatformQualificationDataKey))
                    {
                        var potentialPlatform = addInQualificationData[PlatformQualificationDataKey];
                        if (!Enum.TryParse(potentialPlatform, true, out addInProcessPlatform))
                        {
                            // default to "Host" if the qualification data value couldn't be parsed
                            addInProcessPlatform = Platform.Host;
                        }
                    }
                    const string template =
                        "Add-in named '{0}', version {2}, published by '{1}' and described as '{3}' will be activated out-of-process under platform {4}.";
                    var message = String.Format(template, token.Name, token.Publisher, token.Version, token.Description, addInProcessPlatform);
                    Log.Info(message);

                    var currentToken = token;
                    T addInOfT;
                    try
                    {
                        addInOfT = factory(this, currentToken, addInProcessPlatform);
                    }
                    catch (InvalidOperationException e)
                    {
                        Log.Error(e.Message, e);
                        continue;
                    }
                    result.Add(addInOfT);
                }
            }
            return new ReadOnlyCollection<T>(result);
        }

        public static T DefaultFactory<T>(AddInFacade addInFacade, AddInToken addInToken, Platform addInProcessPlatform)
        {
            var addInProcess = addInFacade.CreateAddInProcess(addInProcessPlatform);
            addInProcess.Start();
            T instance;
            try
            {
                instance = addInToken.Activate<T>(addInProcess, AddInSecurityLevel.FullTrust);
            }
            catch (Exception e)
            {
                const string errorTemplate =
                    "Unable to activate add-in named '{0}', version {2}, published by '{1}' out-of-process under platform {3}.";
                var errorMessage =
                    String.Format(errorTemplate, addInToken.Name, addInToken.Publisher, addInToken.Version, addInProcessPlatform);
                throw new InvalidOperationException(errorMessage, e);
            }
            return instance;
        }

        public AddInProcess CreateAddInProcess(Platform platform)
        {
            var addInProcess = new AddInProcess(platform);
            _addInProcesses.Add(addInProcess);
            return addInProcess;
        }

        public void Dispose()
        {
            foreach (var addInProcess in _addInProcesses)
            {
                addInProcess.Shutdown();
            }
        }
    }
}
