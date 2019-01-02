using System;
using ADL.Util;

namespace ADL.Node.Core.Modules.Network.Messages
{
    public class MessageReplyWait
    {
        public int Attempts { get; set; }
        public DateTime Sent { get; set; }
        public Message Message{ get; private set; }
        public ulong MagicCookie { get; private set; }
        public bool IsTimeout => (DateTimeProvider.UtcNow - Sent).TotalSeconds > 20;

        public MessageReplyWait(Message message, ulong magicCookie)
        {
            Message = message;
            MagicCookie = magicCookie;
            Sent = DateTimeProvider.UtcNow;
            Attempts = 0;
        }
    }
}
