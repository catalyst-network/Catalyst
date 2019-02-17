using System;
using Catalyst.Node.Core.Helpers.Util;
using Catalyst.Node.Core.Modules.P2P.Messages;
using Catalyst.Node.Core.P2P.Messages;

namespace Catalyst.Node.Core.Messages
{
    public class MessageReplyWait
    {
        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="magicCookie"></param>
        public MessageReplyWait(Message message, ulong magicCookie)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (magicCookie <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(magicCookie));
            }
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