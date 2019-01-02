using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using ADL.Node.Core.Modules.Network.Workers;
using System.Linq;

namespace ADL.Node.Core.Modules.Network.Peer
{
     public class PeerList : IEnumerable<Peer>
    {
        public bool IsCritical => _peerList.Count <= 25; // @TODO decide what counts an unhealthy number of peers
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
        /// returns a list of our peers
        /// </summary>
        /// <returns></returns>
        public List<string> ListPeers()
        {
            Dictionary<string, Connection> peers = UnIdentifiedPeers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<string> ret = new List<string>();
            foreach (KeyValuePair<string, Connection> curr in peers)
            {
                Console.WriteLine(curr.Key);
                ret.Add(curr.Key);
            }
            return ret;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool IsPeerConnected(string ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip));
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            
            return UnIdentifiedPeers.TryGetValue(ip+":"+port, out Connection peer);
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
                Log.Log.Message("bot with same ID already exists. Touching it.");
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

            if (!Equals(peerId.Known, false) && IsRegisteredBot(peerId))
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

        public void PurgeConnections()
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
            return sortedBy.GetRange(0, System.Math.Min((int) 8, (int) sortedBy.Count));
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

        internal bool IsRegisteredBot(PeerIdentifier peerId)
        {
            return _peerList.ContainsKey(peerId);
        }

//        public Peer this[PeerIdentifier peerId] => _peerList[peerId];

        public bool TryGet(IPEndPoint endpoint, out Peer peerInfo)
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