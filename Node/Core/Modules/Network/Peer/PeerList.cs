using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using ADL.Node.Core.Modules.Network.Workers;

namespace ADL.Node.Core.Modules.Network.Peer
{
     public class PeerList : IEnumerable<Peer>
    {
        private readonly Dictionary<PeerIdentifier, Peer> _peerList;
  
        /// <summary>
        /// 
        /// </summary>
        /// <param name="worker"></param>
        internal PeerList(IWorkScheduler worker)
        {
            var workScheduler = worker ?? throw new ArgumentNullException(nameof(worker));
            _peerList = new Dictionary<PeerIdentifier, Peer>();

            workScheduler.QueueForever(Check, TimeSpan.FromMinutes(5));
            workScheduler.QueueForever(Purge, TimeSpan.FromSeconds(15));
            workScheduler.QueueForever(Save, TimeSpan.FromMinutes(1));
        }

        private void Check()
        {
            Log.Log.Message("Checking peer list");
            if (!IsCritical) return;
            // go back to peer tracker and ask for more peers
        }

        public bool IsCritical => _peerList.Count <= 25; // @TODO decide what counts an unhealthy number of peers

        public bool TryRegister(Peer peerInfo)
        {
            var endpoint = peerInfo.EndPoint;
            var ip = endpoint.Address;
            var botId = peerInfo.PeerIdentifier;

            if (botId.Equals(BotIdentifier.Id))
            {
                Logger.Verbose("failed attempt for auto-adding");
                return false;
            }

            if (endpoint.Port <= 30000 || endpoint.Port >= 50000)
            {
                Logger.Verbose("failed out-of-range port number ({0}). ", endpoint.Port);
                return false;
            }
            
            if (_peerList.ContainsKey(peerInfo.PeerIdentifier))
            {
                Logger.Verbose("bot with same ID already exists. Touching it.");
                var peer = _peerList[botId];
                peer.EndPoint = endpoint;
                peer.Touch();
                return false;
            }

            if (_peerList.Count >= 256)
            {
                Purge();
            }

            _peerList.Add(botId, peerInfo);
            Logger.Verbose("{0} added", peerInfo);

            var unknown = BotIdentifier.Unknown;
            if (!Equals(botId, unknown) && IsRegisteredBot(unknown) && Equals(this[unknown].EndPoint, peerInfo.EndPoint))
            {
                _peerList.Remove(unknown);
            }
            return true;
        }

        public List<Peer> GetPeersEndPoint()
        {
            return Recent();
        }

        public void UpdatePeer(BotIdentifier botId)
        {
            if (_peerList.ContainsKey(botId))
            {
                _peerList[botId].Touch();
            }
        }

        public void Punish(BotIdentifier botId)
        {
            if (_peerList.ContainsKey(botId))
            {
                _peerList[botId].DecreaseReputation();
            }
        }

        public void Save()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var peerInfo in _peerList.Values)
                {
                    sb.AppendLine(peerInfo.ToString());
                }
                var list = sb.ToString();
                File.WriteAllText("peerlist_" + BotIdentifier.Id + ".txt", list);
            }
            catch
            {
                // ignore if something wrong happened
            }
        }

        public void Load()
        {
            try
            {
                var lines = File.ReadAllLines("peerlist_" + BotIdentifier.Id + ".txt");

                foreach (var line in lines)
                {
                    TryRegister(Peer.Parse(line));
                }
            }
            catch (FileNotFoundException)
            {
                // ignored
            }
        }

        public void Purge()
        {
            var peersInfo = new List<Peer>(_peerList.Values);
            foreach (var peerInfo in peersInfo)
            {
                if (peerInfo.IsAwolBot)
                {
                    _peerList.Remove(peerInfo.PeerIdentifier);
                }
            }
        }

        public List<Peer> Recent()
        {
            var sortedBy = SortedPeers();
            return sortedBy.GetRange(0, Math.Min((int) 8, (int) sortedBy.Count));
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

        internal bool IsRegisteredBot(BotIdentifier botId)
        {
            return _peerList.ContainsKey(botId);
        }

        public Peer this[BotIdentifier botId]
        {
            get { return _peerList[botId]; }
        }

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