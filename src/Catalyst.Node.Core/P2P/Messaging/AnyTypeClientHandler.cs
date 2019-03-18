﻿using System;
using System.Reflection;
using Catalyst.Node.Common.Helpers;
using Catalyst.Protocols.Transaction;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging {
    public class AnyTypeClientHandler : SimpleChannelInboundHandler<Any>
    { 
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void ChannelRead0(IChannelHandlerContext context, Any message)
        {
            var log = JsonConvert.SerializeObject(message);
            Logger.Information(log + Environment.NewLine);

            if (message.TypeUrl == StTx.Descriptor.ShortenedFullName())
            {
                OutputTypedContentAsJson<StTx>(message);
            }
            else if (message.TypeUrl == Key.Descriptor.ShortenedFullName())
            {
                OutputTypedContentAsJson<Key>(message);
            }
            else
            {
                Logger.Warning("Unknown type {0} wrapped in an 'Any' message", message.TypeUrl);
            }
        }

        private static void OutputTypedContentAsJson<T>(Any message) where T : IMessage
        {
            var tx = message.FromAny<T>();
            var innerLog = JsonConvert.SerializeObject(tx);
            Logger.Information(innerLog + Environment.NewLine);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Logger.Error(e, "Error in P2P client");
            context.CloseAsync();
        }
    }
}