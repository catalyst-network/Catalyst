using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.Interfaces.IO
{
    public interface INodeBusinessEventFactory
    {
        IEventLoopGroup NewRpcServerLoopGroup();

        IEventLoopGroup NewUdpServerLoopGroup();

        IEventLoopGroup NewUdpClientLoopGroup();
    }
}
