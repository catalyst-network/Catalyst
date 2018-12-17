using System;
using System.Net;

namespace ADL.Node.Core.Modules.Network.Peer
{
    public class PeerInfo
    {
        public PeerInfo(BotIdentifier botId, IPEndPoint endpoint)
        {
            BotId = botId;
            EndPoint = endpoint;
            LastSeen = DateTimeProvider.UtcNow;
        }
        public BotIdentifier BotId { get; internal set; }
        public IPEndPoint EndPoint { get; set; }
        public DateTime LastSeen { get; set; }
        public int Reputation { get; private set; }
        public byte[] EncryptionKey { get; internal set; }
        public byte[] PublicKey { get; set; }
        public short BotVersion { get; set; }
        public short CfgVersion { get; set; }
        public bool Handshaked { get; set; }
        internal TimeSpan InactiveFor
        {
            get { return DateTimeProvider.UtcNow - LastSeen; }
        }

        public bool IsUnknownBot
        {
            get { return Reputation < -10; }
        }

        public bool IsLazyBot
        {
            get { return InactiveFor > TimeSpan.FromMinutes(30); }
        }

        internal void Touch()
        {
            LastSeen = DateTimeProvider.UtcNow;
        }

        public void DecreseReputation()
        {
            Reputation--;
        }

        public override string ToString()
        {
            return string.Format("{0}@{1}", BotId, EndPoint);
        }

        public static PeerInfo Parse(string line)
        {
            var parts = line.Split('@', ':');
            var id = BotIdentifier.Parse(parts[0]);
            var ip = IPAddress.Parse(parts[1]);
            var port = int.Parse(parts[2]);

            return new PeerInfo(id, new IPEndPoint(ip, port));
        }
    }
}