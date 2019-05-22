using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Messaging
{
    public class SignatureHandler : ObservableHandlerBase<AnySigned>
    {
        private readonly IKeySigner _keySigner;

        public SignatureHandler(IKeySigner keySigner) { _keySigner = keySigner; }

        protected override void ChannelRead0(IChannelHandlerContext ctx, AnySigned msg)
        {
            bool valid = _keySigner.Verify(msg);

            if (valid)
            {
                ctx.FireChannelRead(msg);
            }
        }
    }
}
