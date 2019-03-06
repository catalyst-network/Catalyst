#region LICENSE
/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
* 
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/
#endregion

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
        private readonly PeerIdentifier _peerIdentifier;

        private int Reputation { get; set; }
        private DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        private PeerIdentifier PeerIdentifier => _peerIdentifier;
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
