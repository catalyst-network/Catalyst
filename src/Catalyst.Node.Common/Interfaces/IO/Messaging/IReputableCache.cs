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
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.Interfaces.P2P.Messaging;
using Microsoft.Extensions.Caching.Memory;

namespace Catalyst.Node.Common.Interfaces.IO.Messaging
{
    public interface IReputableCache : IMessageCorrelationCache
    {
        /// <summary>
        /// Stream of reputation changes events raised by requests being answered or expired.
        /// </summary>
        IObservable<IPeerReputationChange> PeerRatingChanges { get; }

        /// <summary>
        ///     Manipulate a peers reputation on eviction from a cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="reason"></param>
        /// <param name="state"></param>
        void ChangeReputationOnEviction(object key, object value, EvictionReason reason, object state);
    }
}
