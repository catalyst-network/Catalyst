using System.Threading.Tasks;
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
            AnySigned msg = (AnySigned) message;
            
            bool valid = _keySigner.Verify(msg);

            if (valid)
            {
                context.FireChannelRead(msg);
            }
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            bool isTcp = !(message is DatagramPacket);
            AnySigned anySigned = isTcp ? (AnySigned) message : null;
            DatagramPacket datagram = null;

            if (!isTcp)
            {
                datagram = (DatagramPacket) message;
                anySigned = AnySigned.Parser.ParseFrom(datagram.Content.Array);
            }

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
