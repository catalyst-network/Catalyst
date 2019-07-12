using Catalyst.Common.Interfaces.P2P;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Common.Interfaces.Rpc.IO.Messaging.Dto
{
    /// <summary>
    ///     Dto to be used to push RPC protocol messages to an observable stream.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRPCClientMessageDto<T> where T : IMessage
    {
        IPeerIdentifier Sender { get; set; }
        T Message { get; set; }
    }
}
