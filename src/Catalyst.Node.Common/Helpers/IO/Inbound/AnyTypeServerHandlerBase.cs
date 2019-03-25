using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces.P2P;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Inbound {
    public class AnyTypeServerHandlerBase :
        SimpleChannelInboundHandler<Any>, IMessageStreamer<ContextAny>, IDisposable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly BehaviorSubject<ContextAny> _messageSubject = new BehaviorSubject<ContextAny>(null);
        public IObservable<ContextAny> MessageStream => _messageSubject.AsObservable();
        public override bool IsSharable => true;

        protected override void ChannelRead0(IChannelHandlerContext ctx, Any msg)
        {
            var contextAny = new ContextAny(msg, ctx);
            _messageSubject.OnNext(contextAny);
        }

        public override void ChannelReadComplete(IChannelHandlerContext ctx) => ctx.Flush();

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Logger.Error(e, "Error in P2P server");
            ctx.CloseAsync().ContinueWith(_ => _messageSubject.OnCompleted());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageSubject?.Dispose();
            }
        }

        public void Dispose() { Dispose(true); }
    }
}