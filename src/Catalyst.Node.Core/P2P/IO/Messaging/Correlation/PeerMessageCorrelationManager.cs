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
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P.ReputationSystem;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Node.Core.P2P.IO.Messaging.Correlation
{
    public sealed class PeerMessageCorrelationManager : MessageCorrelationManagerBase, IPeerMessageCorrelationManager
    {
        private readonly ReplaySubject<IPeerReputationChange> _reputationEvent;
        public ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>> _evictionEvent { get; }
        public IObservable<IPeerReputationChange> ReputationEventStream => _reputationEvent.AsObservable();
        public IObservable<KeyValuePair<ICorrelationId, IPeerIdentifier>> EvictionEventStream => _evictionEvent.AsObservable();

        public PeerMessageCorrelationManager(IReputationManager reputationManager,
            IMemoryCache cache,
            ILogger logger,
            IChangeTokenProvider changeTokenProvider) : base(cache, logger, changeTokenProvider)
        {
            _reputationEvent = new ReplaySubject<IPeerReputationChange>(0);
            _evictionEvent = new ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>>(0);

            reputationManager.MergeReputationStream(ReputationEventStream);
        }

        protected override void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            Logger.Verbose("{key} message evicted", (key as ByteString).ToCorrelationId());
            var message = (CorrelatableMessage<ProtocolMessage>) value;
            
            _evictionEvent.OnNext(new KeyValuePair<ICorrelationId, IPeerIdentifier>(
                new CorrelationId(message.Content.CorrelationId.ToByteArray()),
                message.Recipient)
            );
            
            _reputationEvent.OnNext(
                new PeerReputationChange(message.Recipient,
                    ReputationEvents.NoResponseReceived
                )
            );
        }

        /// <summary>
        ///     Takes a generic request type of IMessage, and generic response type of IMessage and the message and look them up in the cache.
        ///     Return what's found or emit an-uncorrectable event 
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public override bool TryMatchResponse(ProtocolMessage response)
        {
            Guard.Argument(response, nameof(response)).NotNull();

            if (!PendingRequests.TryGetValue(response.CorrelationId, out CorrelatableMessage<ProtocolMessage> message))
            {
                Logger.Debug($"{response.CorrelationId} message not found");

                _reputationEvent.OnNext(new PeerReputationChange(new PeerIdentifier(response.PeerId),
                    ReputationEvents.UnCorrelatableMessage)
                );
                return false;
            }
            
            Logger.Debug($"{response.CorrelationId} message found");
            _reputationEvent.OnNext(new PeerReputationChange(message.Recipient,
                ReputationEvents.ResponseReceived)
            );
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            if (!disposing)
            {
                return;
            }

            _reputationEvent?.Dispose();
        }
    }
}
