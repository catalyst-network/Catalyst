using System;
using Catalyst.Helpers.Logger;
using Catalyst.Node.Modules.Core.P2P.Connections;

namespace Catalyst.Node.Modules.Core.P2P.Events
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
            Log.Message("NewUnIdentifiedConnectionEventArgs");
            Connection = connection;
        }
    }
}
