using System;
using System.Threading.Tasks;

namespace Catalyst.Node.Events.Core
{
    public static class Events
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <typeparam name="T"></typeparam>
        public static Task AsyncRaiseEvent<T>(EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            //@TODO guard util
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return Task.Factory.StartNew(() =>
            {
                handler(sender, args);
            });
        }
    }
}
