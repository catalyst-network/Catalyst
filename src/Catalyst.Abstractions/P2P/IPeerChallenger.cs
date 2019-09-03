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

using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Catalyst.Abstractions.P2P
{
    /// <summary>
    /// This class is used to validate peers by carrying out a peer challenge response
    /// </summary>
    public interface IPeerChallenger 
    {
        /// <summary>
        /// Used to challenge a peer for a response based on the provided public key, ip and port chunks 
        /// </summary>
        /// <param name="recipientPeerIdentifier">The recipient peer identifier.
        /// PeerIdentifier holds the chunks we want to validate.</param>
        /// <returns>bool true means valid and false means not valid</returns>
        Task<bool> ChallengePeerAsync(IPeerIdentifier recipientPeerIdentifier);

        ReplaySubject<IPeerChallengeResponse> ChallengeResponseMessageStreamer { get; }
    }
}
