using System;
using log4net;

namespace Solink.AddIn.Helpers
{
    public abstract class RestartableBase<T, TException>
        where T : class
        where TException : Exception
    {
        public const int MaximumAttempts = 3;
        private static readonly ILog Log = LogManager.GetLogger(typeof (RestartableBase<T, TException>));

        private readonly Type _typeOfT;
        private readonly Type _typeOfTException;
        private readonly Func<T> _factory;
        
        private T _instance;

        protected RestartableBase(Func<T> factory)
        {
            _typeOfT = typeof (T);
            _typeOfTException = typeof (TException);
            _factory = factory;
            _instance = factory();
        }

        protected void Action(Action<T> action)
        {
            Func(t =>
            {
                action(t);
                return t;
            });
        }
 
        protected TResult Func<TResult>(Func<T, TResult> func)
        {
            var result = default(TResult);
            var executed = false;
            TException lastException = null;
            for (var attempt = 0; attempt < MaximumAttempts; attempt++)
            {
                try
                {
                    result = func(_instance);
                    executed = true;
                    break;
                }
                catch (TException e)
                {
                    lastException = e;
                    const string template = "Reactivating '{0}' due to {1}: {2}.";
                    var message = String.Format(template, _typeOfT, _typeOfTException, e);
                    Log.Warn(message);
                    _instance = _factory();
                }
            }
            if (!executed)
            {
                const string template = "Exceeded the maximum number of reactivations ({0}) trying to execute method.";
                var message = String.Format(template, MaximumAttempts);
                throw new InvalidOperationException(message, lastException);
            }
            return result;
        }
    }
}
