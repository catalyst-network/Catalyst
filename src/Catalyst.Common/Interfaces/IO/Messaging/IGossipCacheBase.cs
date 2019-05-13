using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Protocol.Common;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    interface IGossipCacheBase
    {
        bool CanGossip(AnySigned message);

        void Gossip(AnySigned message);
    }
}
