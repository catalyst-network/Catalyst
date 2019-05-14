using System;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    public interface IGossipCacheBase<T> where T : class, IMessage<T>
    {
        bool CanGossip(Guid correlationId);

        void Gossip(IChannel channel, IP2PMessageFactory<T> messageFactoryBase, IChanneledMessage<AnySigned> message, Guid correlationId);
    }
}
