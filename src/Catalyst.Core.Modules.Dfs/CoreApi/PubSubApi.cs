using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using Lib.P2P.PubSub;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class PubSubApi : IPubSubApi
    {
        private readonly IPubSubService _pubSubService;

        public PubSubApi(PubSubService pubSubService)
        {
            _pubSubService = pubSubService;
        }

        public async Task<IEnumerable<Peer>> PeersAsync(string topic = null,
            CancellationToken cancel = default(CancellationToken))
        {
            return await _pubSubService.PeersAsync(topic, cancel);
        }

        public async Task PublishAsync(string topic,
            string message,
            CancellationToken cancel = default(CancellationToken))
        {
            await _pubSubService.PublishAsync(topic, message, cancel);
        }

        public async Task PublishAsync(string topic,
            byte[] message,
            CancellationToken cancel = default(CancellationToken))
        {
            await _pubSubService.PublishAsync(topic, message, cancel);
        }

        public async Task PublishAsync(string topic,
            Stream message,
            CancellationToken cancel = default(CancellationToken))
        {
            await _pubSubService.PublishAsync(topic, message, cancel);
        }

        public async Task SubscribeAsync(string topic,
            Action<IPublishedMessage> handler,
            CancellationToken cancellationToken)
        {
            await _pubSubService.SubscribeAsync(topic, handler, cancellationToken);
        }

        public async Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel =
            default(CancellationToken))
        {
            return await _pubSubService.SubscribedTopicsAsync(cancel);
        }
    }
}
