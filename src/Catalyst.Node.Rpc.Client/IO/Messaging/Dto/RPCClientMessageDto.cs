using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Dto;
using Dawn;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Node.Rpc.Client.IO.Messaging.Dto
{
    public sealed class RPCClientMessageDto<T> : IRPCClientMessageDto<T> where T : IMessage
    {
        public IPeerIdentifier Sender { get; set; }
        public T Message { get; set; }

        public RPCClientMessageDto(T message, IPeerIdentifier sender)
        {
            //Guard.Argument(message, nameof(message))
            //   .Require(message.GetType().Namespace.Contains("IPPN"));
            Message = message;
            Sender = sender;
        }
    }
}
