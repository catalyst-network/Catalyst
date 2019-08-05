using System;

namespace Catalyst.Node.Rpc.Client.IO.Exceptions
{
    public class ResponseHandlerDoesNotExistException : Exception
    {
        public ResponseHandlerDoesNotExistException(string message) : base(message) { }
    }
}
