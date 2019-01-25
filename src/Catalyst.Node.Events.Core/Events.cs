using System;
using System.Threading.Tasks;
using Dawn;

namespace Catalyst.Node.Events.Core
{
    public static class Events
    {
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
            return Task.Factory.StartNew(() => { handler(sender, args); });
        }
    }
}