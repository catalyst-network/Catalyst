using System;
using Catalyst.Node.Common.Helpers.IO;
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

        private Connection Connection { get; }
    }
}