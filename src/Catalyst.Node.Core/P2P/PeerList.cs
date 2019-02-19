using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Catalyst.Node.Core.Events;
using Catalyst.Node.Core.Helpers.IO;
using Catalyst.Node.Core.Helpers.Workers;
using Dawn;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public class PeerList : IEnumerable<Peer>, IDisposable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal readonly ConcurrentDictionary<PeerIdentifier, Peer> PeerBucket;
        internal readonly ConcurrentDictionary<string, Connection> UnIdentifiedPeers;

        /// <summary>
        /// </summary>
        /// <param name="worker"></param>
        internal PeerList(IWorkScheduler worker)
        {
            Guard.Argument(worker, nameof(worker)).NotNull();

            K = 42;
            WorkScheduler = worker;
            PeerBucket =
                new ConcurrentDictionary<PeerIdentifier, Peer>();
            UnIdentifiedPeers = new ConcurrentDictionary<string, Connection>();

            // setup work queues for peer net.
            WorkScheduler.QueueForever(Save, Helpers.Util.TimeExtensions.Minutes(1));
            WorkScheduler.QueueForever(Check, Helpers.Util.TimeExtensions.Minutes(5));
            WorkScheduler.QueueForever(PurgePeers, Helpers.Util.TimeExtensions.Minutes(15));
            //@TODO add a purge for unidentified peers every 10 seconds
            WorkScheduler.Start();
        }

        private uint K { get; }
        private List<IPAddress> BannedIps { get; set; }
        private bool IsCritical => PeerBucket.Count <= 25;
        private IWorkScheduler WorkScheduler { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Peer> GetEnumerator()
        {
            return PeerBucket.Values.GetEnumerator();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event EventHandler<NewUnIdentifiedConnectionEventArgs> OnAddedUnIdentifiedConnection;

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="foundConnection"></param>
        /// <returns></returns>
        public bool IsKnownConnection(Connection connection)
        {
            Guard.Argument(connection, nameof(connection)).NotNull();

            var foundPeerFromConnection = FindPeerFromConnection(connection, out var _);
            
            var endPoint = connection.EndPoint.Address + ":" + connection.EndPoint.Port;
            var foundUnidentifiedConnection = UnIdentifiedPeers.TryGetValue(endPoint,
                out var _);
            
            return foundPeerFromConnection || foundUnidentifiedConnection;
        }

        /// <summary>
        ///     returns a list of unidentified connections
        /// </summary>
        /// <returns></returns>
        public List<string> ListUnidentifiedConnections()
        {
            var ret = new List<string>();
            var peers = UnIdentifiedPeers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach (var curr in peers)
            {
                Logger.Information(curr.Key);
                ret.Add(curr.Key);
            }

            return ret;
        }

        /// <summary>
        /// </summary>
        /// <param name="needle"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        internal bool AddUnidentifiedConnectionToList(Connection needle)
        {
            Guard.Argument(needle, nameof(needle)).NotNull();
            Guard.Argument(needle.EndPoint, nameof(needle.EndPoint)).NotNull();
            Guard.Argument(needle.EndPoint.Address, nameof(needle.EndPoint.Address)).NotNull();

            try
            {
                var endPointAsString = $"{needle.EndPoint.Address}:{needle.EndPoint.Port}";
                if (UnIdentifiedPeers.TryGetValue(endPointAsString, out var connection))
                {
                    // already have a connection in our unidentified list, check if result is actually connected
                    if (connection.IsConnected())
                    {
                        Logger.Debug("*** Active connection already exists for {0}", endPointAsString);
                        return false;
                    }

                    // connection is stale so remove it
                    if (!TryRemoveUnidentifiedConnectionFromList(connection))
                    {
                        Logger.Warning("Cant remove stale connection");
                        needle.Dispose();
                        return false;
                    }

                    Logger.Debug("Removed stale connection for {0}", endPointAsString);
                }

                if (!UnIdentifiedPeers.TryAdd(endPointAsString, needle))
                {
                    Logger.Warning("Can not add unidentified connection to the list");
                    needle.Dispose();
                    return false;
                }
            
            }
            catch (Exception e)
            {
                Logger.Error(e, "AddUnidentifiedConnectionToList: TryAdd");
                needle.Dispose();
                return false;
            }

            try
            {
                Logger.Information("*** Unidentified connection " + needle.EndPoint.Address + needle.EndPoint.Port +
                            " added to unidentified peer list)");
                Events.Events.AsyncRaiseEvent(OnAddedUnIdentifiedConnection, this,
                    new NewUnIdentifiedConnectionEventArgs(needle));
            }
            catch (Exception e)
            {
                needle.Dispose();
                Logger.Error(e, "AddUnidentifiedConnectionToList: Events.Raise(OnAddedUnIdentifiedConnection)");
                return false;
            }
            finally
            {
                needle.Dispose();
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        internal bool TryRemoveUnidentifiedConnectionFromList(Connection connection)
        {
            Guard.Argument(connection, nameof(connection)).NotNull();
            try
            {
                var endPointAsString = $"{connection.EndPoint.Address}:{connection.EndPoint.Port}";
                if (UnIdentifiedPeers.TryRemove(endPointAsString, out var removedConnection))
                {
                    Logger.Information("***** Successfully removed {0}", endPointAsString);
                    return true;
                }

                Logger.Information("*** unable to find connection {0}",endPointAsString);
                return false;
            }
            catch (Exception e)
            {
                Logger.Information(e, "Failed to remove unidentified connection from peer list.");
                return false;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal bool TryRemovePeerFromBucket(Peer peer)
        {
            Guard.Argument(peer, nameof(peer)).NotNull();
            try
            {
                if (!PeerBucket.TryRemove(peer.PeerIdentifier, out var removedPeer))
                {
                    return false;
                }
                Logger.Information("***** Successfully removed {0} from peer bucket.", 
                    removedPeer.PeerIdentifier );
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to remove peer {0} from bucket", peer.PeerIdentifier);
                return false;
            }
        }

        /// <summary>
        /// </summary>
        private void Check()
        {
            Logger.Debug("Checking peer list");
            if (!IsCritical)
            {
                return;
            }
            // @TODO go back to peer tracker and ask for more peers
        }

        /// <summary>
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal bool CheckIfIpBanned(TcpClient tcpClient)
        {
            Guard.Argument(tcpClient, nameof(tcpClient)).NotNull();
            
            var ipAddress = ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Address;

            if (BannedIps?.Count > 0)
            {
                if (!BannedIps.Contains(ipAddress))
                {
                    Logger.Information("*** Rejecting connection from " + ipAddress + " (not permitted)");
                    tcpClient.Dispose();
                    return true;
                }   
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="peerInfo"></param>
        /// <returns></returns>
        public bool TryRegister(Peer peerInfo)
        {
            // we also need to look in our unidentified list
            //@TODO we should pass in connection as we need to establish a relationship between the connection and the peer
            Guard.Argument(peerInfo, nameof(peerInfo)).NotNull();
            
            if (PeerBucket.ContainsKey(peerInfo.PeerIdentifier))
            {
                Logger.Information("peer with same ID already exists. Touching it.");
                var peer = PeerBucket[peerInfo.PeerIdentifier];
                peer.EndPoint = peerInfo.EndPoint;
                peer.Touch();
                return false;
            }

            if (PeerBucket.Count >= 256)
            {
                PurgePeers();
            }

            PeerBucket.TryAdd(peerInfo.PeerIdentifier, peerInfo);
            Logger.Information("{0} added" + peerInfo);

            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public List<Peer> GetPeersEndPoint()
        {
            return Recent();
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UpdatePeer(PeerIdentifier peerId)
        {
            Guard.Argument(peerId, nameof(peerId)).NotNull();

            if (PeerBucket.ContainsKey(peerId))
            {
                PeerBucket[peerId].Touch();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="peer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Punish(Peer peer)
        {
            Guard.Argument(peer, nameof(peer)).NotNull();

            if (PeerBucket.ContainsKey(peer.PeerIdentifier))
            {
                PeerBucket[peer.PeerIdentifier].DecreaseReputation();
            }
        }

        /// <summary>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private static void Save()
        {
            // save peer list from DB
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void Load()
        {
            // load peer list from DB
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void PurgeUnidentified()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        private void PurgePeers()
        {
            var peersInfo = new List<Peer>(PeerBucket.Values);
            foreach (var peerInfo in peersInfo)
            {
                if (peerInfo.IsAwolBot)
                {
                    PeerBucket.TryRemove(peerInfo.PeerIdentifier, out _);
                }                
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private List<Peer> Recent()
        {
            var sortedBy = SortedPeers();
            return sortedBy.GetRange(0, Math.Min(8, sortedBy.Count));
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private List<Peer> SortedPeers()
        {
            var all = new List<Peer>(PeerBucket.Values);
            all.Sort((s1, s2) => (int) (s1.LastSeen - s2.LastSeen).TotalSeconds);
            return all;
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        internal bool IsRegisteredConnection(PeerIdentifier peerId)
        {
            return PeerBucket.ContainsKey(peerId);
        }

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        internal bool FindPeerFromConnection(Connection connection, out Peer peer)
        {
            // iterate peer bucket to find a peer with connection value matches connection param
            foreach (var item in PeerBucket.Values)
            {
                if (!Equals(item.Connection.EndPoint, connection.EndPoint)) continue;
                peer = item;
                return true;
            }

            throw new KeyNotFoundException();
        }

        /// <summary>
        /// </summary>
        public void Clear()
        {
            PeerBucket.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnIdentifiedPeers?.ToList().ForEach(p => p.Value?.Dispose());
                PeerBucket?.ToList().ForEach(p => p.Value?.Dispose());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}