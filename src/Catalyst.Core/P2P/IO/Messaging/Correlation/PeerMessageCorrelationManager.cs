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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.ReputationSystem;
using Catalyst.Abstractions.Types;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P.ReputationSystem;
using Catalyst.Protocol.Common;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Core.P2P.IO.Messaging.Correlation
{
    public sealed class PeerMessageCorrelationManager : MessageCorrelationManagerBase, IPeerMessageCorrelationManager
    {
        private readonly ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>> _evictionEvent;
        private readonly ReplaySubject<IPeerReputationChange> _reputationEvent;

        public PeerMessageCorrelationManager(IReputationManager reputationManager,
            IMemoryCache cache,
            ILogger logger,
            IChangeTokenProvider changeTokenProvider,
            IScheduler scheduler = null) : base(cache, logger, changeTokenProvider)
        {
            var streamScheduler = scheduler ?? Scheduler.Default;
            _reputationEvent = new ReplaySubject<IPeerReputationChange>(0, streamScheduler);
            ReputationEventStream = _reputationEvent.AsObservable();
            _evictionEvent = new ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>>(0, streamScheduler);
            EvictionEventStream = _evictionEvent.AsObservable();

            reputationManager.MergeReputationStream(ReputationEventStream);
        }

        public IObservable<IPeerReputationChange> ReputationEventStream { get; }
        public IObservable<KeyValuePair<ICorrelationId, IPeerIdentifier>> EvictionEventStream { get; }

        /// <summary>
        ///     Takes a generic request type of IMessage, and generic response type of IMessage and the message and look them up in
        ///     the cache.
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

                _reputationEvent.OnNext(new ReputationChange(new PeerIdentifier(response.PeerId),
                    ReputationEventType.UnCorrelatableMessage)
                );
                return false;
            }

            ValidateResponseType(response, message);

            Logger.Debug($"{response.CorrelationId} message found");
            _reputationEvent.OnNext(new ReputationChange(message.Recipient,
                ReputationEventType.ResponseReceived)
            );
            return true;
        }

        protected override void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (!(value is CorrelatableMessage<ProtocolMessage> message))
            {
                Log.Warning("EvictionCallback called with unknown valued {value}", value);
                return;
            }

            var correlationId = message.Content.CorrelationId.ToCorrelationId();
            Logger.Verbose("{correlationId} message originally sent to {peer} is getting evicted", correlationId,
                message.Recipient);

            _reputationEvent.OnNext(new ReputationChange(new PeerIdentifier(message.Content.PeerId),
                ReputationEventType.NoResponseReceived));
            Logger.Verbose("PeerReputationChange sent for {correlationId}", correlationId);

            _evictionEvent.OnNext(new KeyValuePair<ICorrelationId, IPeerIdentifier>(correlationId, message.Recipient));
            Logger.Verbose("EvictionEvent sent for {correlationId}", correlationId);
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
