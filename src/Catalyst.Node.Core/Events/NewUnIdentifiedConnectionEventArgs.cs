using System;
using Catalyst.Node.Core.Helpers.IO;
using Serilog;

namespace Catalyst.Node.Core.Events
{
    public class NewUnIdentifiedConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public NewUnIdentifiedConnectionEventArgs(Connection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            Connection = connection;
        }

        internal Connection Connection { get; set; }
    }
}