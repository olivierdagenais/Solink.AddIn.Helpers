using System.AddIn.Hosting;
using System.Runtime.Remoting;

namespace Solink.AddIn.Helpers
{
    public abstract class RestartableAddIn<T> : RestartableBase<T, RemotingException>
        where T : class
    {
        protected RestartableAddIn(AddInFacade addInFacade, AddInToken addInToken, Platform addInProcessPlatform)
            : base(() => AddInFacade.DefaultFactory<T>(addInFacade, addInToken, addInProcessPlatform))
        {
        }
    }
}
