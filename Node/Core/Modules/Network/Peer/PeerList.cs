using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using ADL.Node.Core.Modules.Network.Workers;
using System.Linq;
using System.Net.Sockets;
using ADL.Node.Core.Modules.Network.Connections;
using ADL.Node.Core.Modules.Network.Events;

namespace ADL.Node.Core.Modules.Network.Peer
{
     public class PeerList : IEnumerable<Peer>
     {
        private uint K { get; }
        private List<IPAddress> BannedIps { get; set; }
        private bool IsCritical => PeerBucket.Count <= 25;
        private IWorkScheduler WorkScheduler { get; set; }
        internal readonly Dictionary<PeerIdentifier, Peer> PeerBucket;
        internal readonly ConcurrentDictionary<string, Connection> UnIdentifiedPeers;
        public event EventHandler<NewUnIdentifiedConnectionEventArgs> OnAddedUnIdentifiedConnection;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="worker"></param>
        internal PeerList(IWorkScheduler worker)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));
            
            K = 42;
            WorkScheduler = worker;
            PeerBucket = new Dictionary<PeerIdentifier, Peer>(); // @TODO put this in thread safe concurrent directory
            UnIdentifiedPeers = new ConcurrentDictionary<string, Connection>();

            // setup work queues for peer net.
            WorkScheduler.QueueForever(Save, TimeSpan.FromMinutes(1));
            WorkScheduler.QueueForever(Check, TimeSpan.FromMinutes(5));
            WorkScheduler.QueueForever(PurgePeers, TimeSpan.FromSeconds(15));
            //@TODO add a purge for unidentified peers every 10 seconds
            WorkScheduler.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="foundConnection"></param>
        /// <returns></returns>
        public bool SearchLists(Connection connection, out Connection foundConnection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            if (FindPeerFromConnection(connection, out Peer foundPeer))
            {
                foundConnection = foundPeer.Connection;
            }

            if (UnIdentifiedPeers.TryGetValue(connection.EndPoint.Address + ":" + connection.EndPoint.Port, out Connection unidentifiedConnection))
            {
                foundConnection = unidentifiedConnection;
            }

            throw new KeyNotFoundException();
        }

        /// <summary>
        /// returns a list of unidentified connections
        /// </summary>
        /// <returns></returns>
        public List<string> ListUnidentifiedConnections()
        {
            List<string> ret = new List<string>();
            Dictionary<string, Connection> peers = UnIdentifiedPeers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            foreach (KeyValuePair<string, Connection> curr in peers)
            {
                Log.Log.Message(curr.Key);
                ret.Add(curr.Key);
            }
            
            return ret;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="needle"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        internal bool AddUnidentifiedConnectionToList(Connection needle)
        {
            if (needle?.EndPoint?.Address == null) throw new ArgumentNullException(nameof (needle));
            
            try
            {
                if (UnIdentifiedPeers.TryGetValue(needle.EndPoint.Address + ":" + needle.EndPoint.Port, out Connection connection))
                {
                    if (connection == null) throw new ArgumentNullException(nameof(connection));
                    // already have a connection in our unidentified list, check if result is actually connected
                    if (connection.IsConnected())
                    {
                        Log.Log.Message("*** Active connection already exists for " + connection.EndPoint.Address + connection.EndPoint.Port);
                        return false;
                    }
                    try
                    {
                        // connection is stale so remove it
                        if (!RemoveUnidentifiedConnectionFromList(connection))
                        {
                            throw new Exception("Cant remove stale connection");
                        }

                        Log.Log.Message("Removed stale connection for  " + connection.EndPoint.Address + connection.EndPoint.Port);
                    }
                    catch (ArgumentNullException e)
                    {
                        Log.LogException.Message("AddUnidentifiedConnectionToList: RemoveUnidentifiedConnectionFromList", e);
                        needle.Dispose();
                        return false;
                    }
                }
            }
            catch (ArgumentException e)
            {
                Log.LogException.Message("AddUnidentifiedConnectionToList: TryGetValue", e);
                needle.Dispose();
                return false;
            }
            
            try
            {
                if (!UnIdentifiedPeers.TryAdd(needle.EndPoint.Address + ":" + needle.EndPoint.Port, needle))
                {
                    throw new Exception("Can not add unidentified connection to the list");
                }
            }
            catch (Exception e)
            {
                Log.LogException.Message("AddUnidentifiedConnectionToList: TryAdd", e);
                needle.Dispose();
                return false;
            }

            try
            {
                Log.Log.Message("*** Unidentified connection " + needle.EndPoint.Address + needle.EndPoint.Port + " added to unidentified peer list)");
                Util.Events.Raise(OnAddedUnIdentifiedConnection, this, new NewUnIdentifiedConnectionEventArgs(needle));
            }
            catch (ArgumentNullException e)
            {
                Log.LogException.Message("AddUnidentifiedConnectionToList: Events.Raise(OnAddedUnIdentifiedConnection)", e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        internal bool RemoveUnidentifiedConnectionFromList(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof (connection));
            try
            {
                if (UnIdentifiedPeers.TryRemove(connection.EndPoint.Address + ":" + connection.EndPoint.Port, out Connection removedConnection))
                {
                    Log.Log.Message("***** Successfully removed " + removedConnection.EndPoint.Address + removedConnection.EndPoint.Port);
                    return true;
                }
                Log.Log.Message("*** unable to find connection " + connection.EndPoint.Address+":"+connection.EndPoint.Port);
                return false;
            }
            catch (ArgumentNullException e)
            {
                Log.LogException.Message("RemoveUnidentifiedConnectionToList", e);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal bool RemovePeerFromBucket(Peer peer)
        {
            if (peer == null) throw new ArgumentNullException(nameof (peer));
            try
            {
                if (!PeerBucket.Remove(peer.PeerIdentifier))
                {
                    return false;
                }
                Log.Log.Message("***** Successfully removed " + peer.PeerIdentifier.Id + " from peer bucket");
                return true;
            }
            catch (ArgumentNullException e)
            {
                Log.LogException.Message("RemovePeerFromBucket", e);
                return false;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void Check()
        {
            Log.Log.Message("Checking peer list");
            if (!IsCritical) return;
            // @TODO go back to peer tracker and ask for more peers
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal bool CheckIfIpBanned(TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));
            IPAddress ipAddress = ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Address;

            if (BannedIps?.Count > 0)
            {
                if (!BannedIps.Contains(ipAddress))
                {
                    Log.Log.Message("*** Rejecting connection from " + ipAddress + " (not permitted)");
                    tcpClient.Dispose();
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerInfo"></param>
        /// <returns></returns>
        public bool TryRegister(Peer peerInfo)
        {
            // we also need to look in our unidentified list
            //@TODO we should pass in connection as we need to establish a relationship between the connection and the peer
            
            if (peerInfo == null) throw new ArgumentNullException(nameof (peerInfo));

            if (PeerBucket.ContainsKey(peerInfo.PeerIdentifier))
            {
                Log.Log.Message("peer with same ID already exists. Touching it.");
                var peer = PeerBucket[peerInfo.PeerIdentifier];
                peer.EndPoint = peerInfo.EndPoint;
                peer.Touch();
                return false;
            }

            if (PeerBucket.Count >= 256)
            {
                PurgePeers();
            }

            PeerBucket.Add(peerInfo.PeerIdentifier, peerInfo);
            Log.Log.Message("{0} added" + peerInfo);

//            if (!Equals(peerInfo.Known, false) && IsRegisteredConnection(peerId))
//            {
//                PeerBucket.Remove(peerId);
//            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Peer> GetPeersEndPoint()
        {
            return Recent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UpdatePeer(PeerIdentifier peerId)
        {
            if (peerId == null) throw new ArgumentNullException(nameof (peerId));
            if (PeerBucket.ContainsKey(peerId))
            {
                PeerBucket[peerId].Touch();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Punish(Peer peer)
        {
            if (peer == null) throw new ArgumentNullException(nameof (peer));
            if (PeerBucket.ContainsKey(peer.PeerIdentifier))
            {
                PeerBucket[peer.PeerIdentifier].DecreaseReputation();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void Save()
        {
            // save peer list from DB
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Load()
        {
            // load peer list from DB
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void PurgeUnidentified()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        private void PurgePeers()
        {
            var peersInfo = new List<Peer>(PeerBucket.Values);
            foreach (var peerInfo in peersInfo)
            {
                if (peerInfo.IsAwolBot) //@TODO check if connected
                {
                    PeerBucket.Remove(peerInfo.PeerIdentifier);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private List<Peer> Recent()
        {
            var sortedBy = SortedPeers();
            return sortedBy.GetRange(0, System.Math.Min(8, sortedBy.Count));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private List<Peer> SortedPeers()
        {
            var all = new List<Peer>(PeerBucket.Values);
            all.Sort((s1, s2) => (int)(s1.LastSeen - s2.LastSeen).TotalSeconds);
            return all;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Peer> GetEnumerator()
        {
            return PeerBucket.Values.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        internal bool IsRegisteredConnection(PeerIdentifier peerId)
        {
            return PeerBucket.ContainsKey(peerId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        internal bool FindPeerFromConnection(Connection connection, out Peer peer)
        {
            // iterate peer bucket to find a peer with connection value matches connection param
            foreach (Peer item in PeerBucket.Values)
            {
                if (Equals(item.Connection.EndPoint, connection.EndPoint))
                {
                    peer = item;
                    return true;
                }
            }
            throw new KeyNotFoundException();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            PeerBucket.Clear();
        }
    }
}