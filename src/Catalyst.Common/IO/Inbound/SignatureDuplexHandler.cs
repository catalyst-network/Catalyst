using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;

namespace Catalyst.Common.IO.Inbound
{
    public class SignatureDuplexHandler : ChannelDuplexHandler
    {
        private readonly IKeySigner _keySigner;

        public SignatureDuplexHandler(IKeySigner keySigner) { _keySigner = keySigner; }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            bool isUdp = message is DatagramPacket;

            AnySigned msg = isUdp
                ? ((DatagramPacket) message).ToAnySigned()
                : (AnySigned) message;

            bool valid = _keySigner.Verify(msg);

            if (valid)
            {
                context.FireChannelRead(msg);
            }
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            bool isTcp = !(message is DatagramPacket);
            DatagramPacket datagram = !isTcp ? ((DatagramPacket) message) : null;
            AnySigned anySigned = isTcp ? (AnySigned) message : datagram.ToAnySigned();

            anySigned.Signature = _keySigner
               .Sign(anySigned.Value.ToByteArray())
               .Bytes.RawBytes.ToByteString();

            if (isTcp)
            {
                return base.WriteAsync(context, anySigned);
            }
            else
            {
                DatagramPacket packet = new DatagramPacket(
                    Unpooled.CopiedBuffer(anySigned.ToByteArray()), datagram.Sender, datagram.Recipient);
                return base.WriteAsync(context, packet);
            }
        }
    }
}
