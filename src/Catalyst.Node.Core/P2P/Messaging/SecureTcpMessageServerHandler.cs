using System;
using System.Reflection;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging
{
    internal class SecureTcpMessageServerHandler : SimpleChannelInboundHandler<string>
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        private static volatile IChannelGroup _broadCastGroup;
        private readonly object _groupLock = new object();


        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            if (_broadCastGroup == null)
            {
                lock (_groupLock)
                {
                    if (_broadCastGroup == null)
                    {
                        _broadCastGroup = new DefaultChannelGroup(context.Executor);
                    }
                }
            }
            context.WriteAndFlushAsync(string.Format("Welcome to {0} secure chat server!\n", System.Net.Dns.GetHostName()));
            _broadCastGroup.Add(context.Channel);
        }

        private class EveryOneBut : IChannelMatcher
        {

            private readonly IChannelId _id;

            public EveryOneBut(IChannelId id)
            {
                _id = id;
            }

            public bool Matches(IChannel channel) => !Equals(channel.Id, _id);
        }

        protected override void ChannelRead0(IChannelHandlerContext context, string msg)
        {
            //send message to all but this one
            var broadcast = $"[{context.Channel.RemoteAddress}] {msg}\n";
            var response = $"[you] {msg}\n";

            _broadCastGroup.WriteAndFlushAsync(broadcast, new EveryOneBut(context.Channel.Id));
            context.WriteAndFlushAsync(response);

            if (string.Equals("bye", msg, StringComparison.OrdinalIgnoreCase))
            {
                context.CloseAsync();
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext ctx) => ctx.Flush();

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Logger.Error(e, "Error in P2P server");
            ctx.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}