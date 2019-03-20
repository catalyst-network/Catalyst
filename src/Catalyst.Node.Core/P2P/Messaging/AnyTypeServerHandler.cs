using System;
using System.Reflection;
using Catalyst.Node.Common.Helpers;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging
{
    internal class AnyTypeServerHandler : SimpleChannelInboundHandler<Any>
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
            context.WriteAndFlushAsync($"Welcome to {System.Net.Dns.GetHostName()} secure chat server!\n");
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

        protected override void ChannelRead0(IChannelHandlerContext context, Any message)
        {
            _broadCastGroup.WriteAndFlushAsync(message, new EveryOneBut(context.Channel.Id));
            context.WriteAndFlushAsync(message);

            if (message.TypeUrl == Transaction.Descriptor.ShortenedFullName() && message.FromAny<Transaction>().Version == 0)
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