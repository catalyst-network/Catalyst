using System;
using System.Net;
using System.Reflection;
using Catalyst.Node.Common.Helpers.Util;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public class Peer : IDisposable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        private int Reputation { get; set; }
        private DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        private PeerIdentifier PeerIdentifier { get; set; }
        public bool IsAwolBot => InactiveFor > TimeSpan.FromMinutes(30);
        private TimeSpan InactiveFor => DateTimeProvider.UtcNow - LastSeen;

        public void Dispose() { Dispose(true); }

        /// <summary>
        /// </summary>
        internal void Touch() { LastSeen = DateTimeProvider.UtcNow; }

        /// <summary>
        /// </summary>
        public void IncreaseReputation() { Reputation++; }

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
                Logger.Verbose("Connection to peer {0} Disposed.",
                    PeerIdentifier?.Id?.ToString() ?? "unknown");
            }
        }
    }
}
