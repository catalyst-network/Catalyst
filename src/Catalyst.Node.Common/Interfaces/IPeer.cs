using System;
using System.Net;

namespace Catalyst.Node.Common.Interfaces {
    public interface IPeer {
        int Reputation { get; set; }
        DateTime LastSeen { get; set; }
        IPEndPoint EndPoint { get; set; }
        IPeerIdentifier PeerIdentifier { get; }
        bool IsAwolBot { get; }
        TimeSpan InactiveFor { get; }

        /// <summary>
        /// </summary>
        void Touch();

        /// <summary>
        /// </summary>
        void IncreaseReputation();

        /// <summary>
        /// </summary>
        void DecreaseReputation();
    }
}