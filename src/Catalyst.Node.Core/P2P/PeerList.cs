using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Catalyst.Node.Core.Events;
using Catalyst.Node.Core.Helpers.IO;
using Catalyst.Node.Core.Helpers.Logger;
using Catalyst.Node.Core.Helpers.Workers;
using Catalyst.Node.Core.Modules.P2P;
using Dawn;

namespace Catalyst.Node.Core.P2P
{
    public class PeerList : IEnumerable<Peer>
    {
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
        public bool SearchLists(Connection connection, out Connection foundConnection)
        {
            Guard.Argument(connection, nameof(connection)).NotNull();

            if (FindPeerFromConnection(connection, out var foundPeer)) foundConnection = foundPeer.Connection;

            if (UnIdentifiedPeers.TryGetValue(connection.EndPoint.Address + ":" + connection.EndPoint.Port,
                out var unidentifiedConnection)) foundConnection = unidentifiedConnection;

            throw new KeyNotFoundException();
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
                Log.Message(curr.Key);
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
                if (UnIdentifiedPeers.TryGetValue(needle.EndPoint.Address + ":" + needle.EndPoint.Port,
                    out var connection))
                {
                    if (connection == null) throw new ArgumentNullException(nameof(connection));
                    // already have a connection in our unidentified list, check if result is actually connected
                    if (connection.IsConnected())
                    {
                        Log.Message("*** Active connection already exists for " + connection.EndPoint.Address +
                                    connection.EndPoint.Port);
                        return false;
                    }

                    try
                    {
                        // connection is stale so remove it
                        if (!RemoveUnidentifiedConnectionFromList(connection))
                            throw new Exception("Cant remove stale connection");

                        Log.Message("Removed stale connection for  " + connection.EndPoint.Address +
                                    connection.EndPoint.Port);
                    }
                    catch (ArgumentNullException e)
                    {
                        LogException.Message("AddUnidentifiedConnectionToList: RemoveUnidentifiedConnectionFromList",
                            e);
                        needle.Dispose();
                        return false;
                    }
                }
            }
            catch (ArgumentException e)
            {
                LogException.Message("AddUnidentifiedConnectionToList: TryGetValue", e);
                needle.Dispose();
                return false;
            }

            try
            {
                if (!UnIdentifiedPeers.TryAdd(needle.EndPoint.Address + ":" + needle.EndPoint.Port, needle))
                    throw new Exception("Can not add unidentified connection to the list");
            }
            catch (Exception e)
            {
                LogException.Message("AddUnidentifiedConnectionToList: TryAdd", e);
                needle.Dispose();
                return false;
            }

            try
            {
                Log.Message("*** Unidentified connection " + needle.EndPoint.Address + needle.EndPoint.Port +
                            " added to unidentified peer list)");
                Events.Events.AsyncRaiseEvent(OnAddedUnIdentifiedConnection, this,
                    new NewUnIdentifiedConnectionEventArgs(needle));
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("AddUnidentifiedConnectionToList: Events.Raise(OnAddedUnIdentifiedConnection)", e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        internal bool RemoveUnidentifiedConnectionFromList(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            try
            {
                if (UnIdentifiedPeers.TryRemove(connection.EndPoint.Address + ":" + connection.EndPoint.Port,
                    out var removedConnection))
                {
                    Log.Message("***** Successfully removed " + removedConnection.EndPoint.Address +
                                removedConnection.EndPoint.Port);
                    return true;
                }

                Log.Message("*** unable to find connection " + connection.EndPoint.Address + ":" +
                            connection.EndPoint.Port);
                return false;
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("RemoveUnidentifiedConnectionToList", e);
                return false;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal bool RemovePeerFromBucket(Peer peer)
        {
            if (peer == null) throw new ArgumentNullException(nameof(peer));
            try
            {
                if (!PeerBucket.TryRemove(peer.PeerIdentifier, out var removedPeer)) return false;
                Log.Message("***** Successfully removed " + removedPeer.PeerIdentifier + " from peer bucket");
                return true;
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("RemovePeerFromBucket", e);
                return false;
            }
        }

        /// <summary>
        /// </summary>
        private void Check()
        {
            Log.Message("Checking peer list");
            if (!IsCritical) return;
            // @TODO go back to peer tracker and ask for more peers
        }

        /// <summary>
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal bool CheckIfIpBanned(TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));
            var ipAddress = ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Address;

            if (BannedIps?.Count > 0)
                if (!BannedIps.Contains(ipAddress))
                {
                    Log.Message("*** Rejecting connection from " + ipAddress + " (not permitted)");
                    tcpClient.Dispose();
                    return true;
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

            if (peerInfo == null) throw new ArgumentNullException(nameof(peerInfo));

            if (PeerBucket.ContainsKey(peerInfo.PeerIdentifier))
            {
                Log.Message("peer with same ID already exists. Touching it.");
                var peer = PeerBucket[peerInfo.PeerIdentifier];
                peer.EndPoint = peerInfo.EndPoint;
                peer.Touch();
                return false;
            }

            if (PeerBucket.Count >= 256) PurgePeers();

            PeerBucket.TryAdd(peerInfo.PeerIdentifier, peerInfo);
            Log.Message("{0} added" + peerInfo);

            //            if (!Equals(peerInfo.Known, false) && IsRegisteredConnection(peerId))
            //            {
            //                PeerBucket.Remove(peerId);
            //            }
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
            if (peerId == null) throw new ArgumentNullException(nameof(peerId));
            if (PeerBucket.ContainsKey(peerId)) PeerBucket[peerId].Touch();
        }

        /// <summary>
        /// </summary>
        /// <param name="peer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Punish(Peer peer)
        {
            if (peer == null) throw new ArgumentNullException(nameof(peer));
            if (PeerBucket.ContainsKey(peer.PeerIdentifier)) PeerBucket[peer.PeerIdentifier].DecreaseReputation();
        }

        /// <summary>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void Save()
        {
            // save peer list from DB
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Load()
        {
            // load peer list from DB
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void PurgeUnidentified()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        private void PurgePeers()
        {
            var peersInfo = new List<Peer>(PeerBucket.Values);
            foreach (var peerInfo in peersInfo)
                if (peerInfo.IsAwolBot) //@TODO check if connected
                    PeerBucket.TryRemove(peerInfo.PeerIdentifier, out _);
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
                if (Equals(item.Connection.EndPoint, connection.EndPoint))
                {
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
    }
}