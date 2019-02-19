using System;
using Catalyst.Node.Core.Helpers.IO;
using Dawn;

namespace Catalyst.Node.Core.Events
{
    public class NewUnIdentifiedConnectionEventArgs : EventArgs
    {
        public NewUnIdentifiedConnectionEventArgs(Connection connection)
        {
            Guard.Argument(connection, nameof(connection)).NotNull();
            Connection = connection;
        }

        internal Connection Connection { get; }
    }
}