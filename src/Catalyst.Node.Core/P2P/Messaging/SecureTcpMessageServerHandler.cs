using System;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;

namespace Catalyst.Node.Core.P2P.Messaging
{
    internal class SecureTcpMessageServerHandler : SimpleChannelInboundHandler<string>
    {
        static volatile IChannelGroup _group;

        public override void ChannelActive(IChannelHandlerContext contex)
        {
            var g = _group;
            if (g == null)
            {
                lock (this)
                {
                    if (_group == null)
                    {
                        g = _group = new DefaultChannelGroup(contex.Executor);
                    }
                }
            }

            contex.WriteAndFlushAsync(string.Format("Welcome to {0} secure chat server!\n", System.Net.Dns.GetHostName()));
            g.Add(contex.Channel);
        }

        class EveryOneBut : IChannelMatcher
        {
            readonly IChannelId id;

            public EveryOneBut(IChannelId id)
            {
                this.id = id;
            }

            public bool Matches(IChannel channel) => channel.Id != this.id;
        }

        protected override void ChannelRead0(IChannelHandlerContext contex, string msg)
        {
            //send message to all but this one
            var broadcast = string.Format("[{0}] {1}\n", contex.Channel.RemoteAddress, msg);
            var response = string.Format("[you] {0}\n", msg);
            _group.WriteAndFlushAsync(broadcast, new EveryOneBut(contex.Channel.Id));
            contex.WriteAndFlushAsync(response);

            if (string.Equals("bye", msg, StringComparison.OrdinalIgnoreCase))
            {
                contex.CloseAsync();
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext ctx) => ctx.Flush();

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);
            ctx.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}