using System;
using System.Net;
using Catalyst.Node.Core.Helpers.IO;
using Catalyst.Node.Core.Helpers.Util;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public class Peer : IDisposable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// </summary>
        /// <param name="peerIdentifier"></param>
        /// <param name="endpoint"></param>
        /// <exception cref="ArgumentException"></exception>
        // public Peer(PeerIdentifier peerIdentifier, IPEndPoint endpoint)
        // {
        //     Guard.Argument(peerIdentifier, nameof(peerIdentifier)).NotNull();
        //     Guard.Argument(EndPoint.Port, nameof(EndPoint.Port)).Min(1025).Max(123);
        //     EndPoint = endpoint;
        //     PeerIdentifier = peerIdentifier;
        //     LastSeen = DateTimeProvider.UtcNow;
        // }

        private int Reputation { get; set; }
        private bool Disposed { get; set; }
        public DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public Connection Connection { get; set; }
        public PeerIdentifier PeerIdentifier { get; }
        public bool IsAwolBot => InactiveFor > TimeSpan.FromMinutes(30);
        private TimeSpan InactiveFor => DateTimeProvider.UtcNow - LastSeen;

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            Logger.Verbose("disposing peer class");
            GC.SuppressFinalize(this);
        }

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

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                //Connection.Dispose();
            }

            Disposed = true;
            Logger.Verbose($"Peer {PeerIdentifier.Id} disposed");
        }
    }
}