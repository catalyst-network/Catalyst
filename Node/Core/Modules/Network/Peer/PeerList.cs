using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using ADL.Node.Core.Modules.Network.Workers;
using System.Linq;
using System.Net.Sockets;

namespace ADL.Node.Core.Modules.Network.Peer
{
     public class PeerList : IEnumerable<Peer>
    {
        internal List<IPAddress> BannedIps { get; set; }
        public bool IsCritical => _peerList.Count <= 25;
        internal readonly Dictionary<PeerIdentifier, Peer> _peerList;
        internal ConcurrentDictionary<string, Connection> UnIdentifiedPeers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="worker"></param>
        internal PeerList(IWorkScheduler worker)
        {
            var workScheduler = worker ?? throw new ArgumentNullException(nameof(worker));
            _peerList = new Dictionary<PeerIdentifier, Peer>();

            workScheduler.QueueForever(Check, TimeSpan.FromMinutes(5));
            workScheduler.QueueForever(PurgePeers, TimeSpan.FromSeconds(15));
            workScheduler.QueueForever(Save, TimeSpan.FromMinutes(1));
            workScheduler.Start();
        }

        /// <summary>
        /// returns a list of unidentified connections
        /// </summary>
        /// <returns></returns>
        public List<string> ListUnidentifiedConnections()
        {
            Dictionary<string, Connection> peers = UnIdentifiedPeers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<string> ret = new List<string>();
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
            if (needle == null) throw new ArgumentNullException(nameof (needle));
            try
            {
                if (UnIdentifiedPeers.TryGetValue(needle.EndPoint.Address + ":" + needle.EndPoint.Port, out Connection connection))
                {
                    // already have a connection in our unidentified list, check if result is actually connected
                    if (connection.IsConnected())
                    {
                        Log.Log.Message("*** Active connection already exists for " + connection.EndPoint.Address + connection.EndPoint.Port);
                        return false;                        
                    }
                    
                    // connection is stale so remove it
                    if (!RemoveUnidentifiedConnectionFromList(connection))
                    {
                        throw new Exception("Cant remove stale connection");
                    }
                    Log.Log.Message("Removed stale connection for  " + connection.EndPoint.Address + connection.EndPoint.Port);
                }

                if (!UnIdentifiedPeers.TryAdd(needle.EndPoint.Address + ":" + needle.EndPoint.Port, needle))
                {
                    throw new Exception("Can not add unidentified connection to the list");
                }
                
                Log.Log.Message("*** Unidentified connection " + needle.EndPoint.Address + needle.EndPoint.Port + " added to unidentified peer list)");
                return true;
            }
            catch (ArgumentNullException e)
            {
                Log.LogException.Message("TryAddConnectionToList", e);
                needle.Dispose();
                throw new Exception(e.Message);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        private bool RemoveUnidentifiedConnectionFromList(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            try
            {
                if (UnIdentifiedPeers.TryRemove(connection.EndPoint.Address + ":" + connection.EndPoint.Port, out Connection removedConnection))
                {
                    removedConnection.Dispose();
                    Log.Log.Message(removedConnection + "Connection removed");
                    return true;
                }
            }
            catch (ArgumentNullException e)
            {
                Log.LogException.Message("RemoveUnidentifiedConnectionToList", e);
                throw new Exception(e.Message);
            }
            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void Check()
        {
            Log.Log.Message("Checking peer list");
            if (!IsCritical) return;
            // go back to peer tracker and ask for more peers
        }

        internal bool CheckIfIpBanned(TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));
            var ipAddress = ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Address;

            if (BannedIps?.Count > 0)
            {
                if (!BannedIps.Contains(ipAddress))
                {
                    Log.Log.Message("*** AcceptConnections rejecting connection from " + ipAddress + " (not permitted)");
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
            var endpoint = peerInfo.EndPoint;
            var peerId = peerInfo.PeerIdentifier;

            if (_peerList.ContainsKey(peerInfo.PeerIdentifier))
            {
                Log.Log.Message("peer with same ID already exists. Touching it.");
                var peer = _peerList[peerId];
                peer.EndPoint = endpoint;
                peer.Touch();
                return false;
            }

            if (_peerList.Count >= 256)
            {
                PurgePeers();
            }

            _peerList.Add(peerId, peerInfo);
            Log.Log.Message("{0} added" + peerInfo);

            if (!Equals(peerId.Known, false) && IsRegisteredConnection(peerId))
            {
                _peerList.Remove(peerId);
            }
            return true;
        }

        public List<Peer> GetPeersEndPoint()
        {
            return Recent();
        }

        public void UpdatePeer(PeerIdentifier botId)
        {
            if (_peerList.ContainsKey(botId))
            {
                _peerList[botId].Touch();
            }
        }

        public void Punish(PeerIdentifier peerId)
        {
            if (_peerList.ContainsKey(peerId))
            {
                _peerList[peerId].DecreaseReputation();
            }
        }

        public void Save()
        {
            // save peer list from DB
            throw new NotImplementedException();
        }

        public void Load()
        {
            // load peer list from DB
            throw new NotImplementedException();
        }

        public void PurgeUnidentified()
        {
            throw new NotImplementedException();
        }

        public void PurgePeers()
        {
            var peersInfo = new List<Peer>(_peerList.Values);
            foreach (var peerInfo in peersInfo)
            {
                if (peerInfo.IsAwolBot) //@TODO check if connected
                {
                    _peerList.Remove(peerInfo.PeerIdentifier);
                }
            }
        }

        public List<Peer> Recent()
        {
            var sortedBy = SortedPeers();
            return sortedBy.GetRange(0, System.Math.Min(8, sortedBy.Count));
        }

        private List<Peer> SortedPeers()
        {
            var all = new List<Peer>(_peerList.Values);
            all.Sort((s1, s2) => (int)(s1.LastSeen - s2.LastSeen).TotalSeconds);
            return all;
        }

        public IEnumerator<Peer> GetEnumerator()
        {
            return _peerList.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal bool IsRegisteredConnection(PeerIdentifier peerId)
        {
            return _peerList.ContainsKey(peerId);
        }

        public bool FindByEndpoint(IPEndPoint endpoint, out Peer peerInfo)
        {
            foreach (var pi in _peerList.Values)
            {
                if (Equals(pi.EndPoint, endpoint))
                {
                    peerInfo = pi;
                    return true;
                }
            }
            peerInfo = null;
            return false;
        }

        public void Clear()
        {
            _peerList.Clear();
        }
    }
}