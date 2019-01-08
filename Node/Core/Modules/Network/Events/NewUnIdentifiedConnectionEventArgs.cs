using System;
using ADL.Node.Core.Modules.Network.Connections;

namespace ADL.Node.Core.Modules.Network.Events
{
    public class NewUnIdentifiedConnectionEventArgs : EventArgs
    {
        internal Connection Connection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public NewUnIdentifiedConnectionEventArgs(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof (connection));
            Log.Log.Message("NewUnIdentifiedConnectionEventArgs");
            Connection = connection;
        }
    }
}
