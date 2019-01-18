using System;
using Catalyst.Helpers.Util;

namespace Catalyst.Node.Modules.Core.P2P.Messages
{
    public class MessageReplyWait
    {
        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="magicCookie"></param>
        public MessageReplyWait(Message message, ulong magicCookie)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (magicCookie <= 0) throw new ArgumentOutOfRangeException(nameof(magicCookie));
            Attempts = 0;
            Message = message;
            MagicCookie = magicCookie;
            Sent = DateTimeProvider.UtcNow;
        }

        public int Attempts { get; set; }
        public DateTime Sent { get; set; }
        public Message Message { get; }
        public ulong MagicCookie { get; }
        public ulong CorrelationId { get; private set; }
        public bool IsTimeout => (DateTimeProvider.UtcNow - Sent).TotalSeconds > 20;

        //@TODO generate a correlationID
    }
}