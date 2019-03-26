/*
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

using System;
using System.Net;
using System.Reflection;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public class Peer : IDisposable, IPeer
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        public int Reputation { get; set; }
        public DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public IPeerIdentifier PeerIdentifier { get; }
        public bool IsAwolBot => InactiveFor > TimeSpan.FromMinutes(30);
        public TimeSpan InactiveFor => DateTimeUtil.UtcNow - LastSeen;

        public void Dispose() { Dispose(true); }

        /// <summary>
        /// </summary>
        public void Touch() { LastSeen = DateTimeUtil.UtcNow; }

        /// <summary>
        /// </summary>
        public void IncreaseReputation() { Reputation++; }

        /// <summary>
        /// </summary>
        public void DecreaseReputation()
        {
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
