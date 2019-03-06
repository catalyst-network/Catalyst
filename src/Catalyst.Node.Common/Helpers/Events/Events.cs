using System;
using System.Reflection;
using System.Threading.Tasks;
using Dawn;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Events
{
    public static class Events
    {
        private static readonly ILogger Logger = Log.Logger
           .ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <typeparam name="T"></typeparam>
        public static Task AsyncRaiseEvent<T>(EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            Guard.Argument(args, nameof(args)).NotNull();
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(handler, nameof(handler)).NotNull();
            var asyncRaiseEvent = Task.Factory.StartNew(() => { handler(sender, args); });
            Logger.Debug("Raised async event of type {0}", typeof(T));
            return asyncRaiseEvent;
        }
    }
}