using System;
using System.Threading.Tasks;

namespace ADL.Util
{
    internal static class Events
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <typeparam name="T"></typeparam>
        internal static void RaiseAsync<T>(EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Task.Factory.StartNew(() =>
            {
                handler(sender, args);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <typeparam name="T"></typeparam>
        internal static void Raise<T>(EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            handler(sender, args);
        }
    }
}
