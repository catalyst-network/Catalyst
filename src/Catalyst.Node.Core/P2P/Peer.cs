using System;
using System.Net;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.Util;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public class Peer : IDisposable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int Reputation { get; set; }
        private DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        private Connection Connection { get; set; }
        private PeerIdentifier PeerIdentifier { get; set; }
        public bool IsAwolBot => InactiveFor > TimeSpan.FromMinutes(30);
        private TimeSpan InactiveFor => DateTimeProvider.UtcNow - LastSeen;

        /// <summary>
        /// </summary>
        internal void Touch()
        {
            LastSeen = DateTimeProvider.UtcNow;
        }

        /// <summary>
        /// </summary>
        public void IncreaseReputation()
        {
            Reputation++;
        }

        /// <summary>
        /// </summary>
        public void DecreaseReputation()
        {
            //@TODO check if this is bellow ban threshold
            Reputation--;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Connection?.Dispose();
                Logger.Verbose("Connection to peer {0} Disposed.", 
                    PeerIdentifier?.Id?.ToString() ?? "unknown");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    class PeerImpl : Peer { }
}
