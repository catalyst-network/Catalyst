using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using Lib.P2P.PubSub;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    class PubSubApi : IPubSubApi
    {
        IDfs ipfs;

        public PubSubApi(IDfs ipfs) { this.ipfs = ipfs; }

        public async Task<IEnumerable<Peer>> PeersAsync(string topic = null,
            CancellationToken cancel = default(CancellationToken))
        {
            var pubsub = await ipfs.PubSubService.ConfigureAwait(false);
            return await pubsub.PeersAsync(topic, cancel);
        }

        public async Task PublishAsync(string topic,
            string message,
            CancellationToken cancel = default(CancellationToken))
        {
            var pubsub = await ipfs.PubSubService.ConfigureAwait(false);
            await pubsub.PublishAsync(topic, message, cancel);
        }

        public async Task PublishAsync(string topic,
            byte[] message,
            CancellationToken cancel = default(CancellationToken))
        {
            var pubsub = await ipfs.PubSubService.ConfigureAwait(false);
            await pubsub.PublishAsync(topic, message, cancel);
        }

        public async Task PublishAsync(string topic,
            Stream message,
            CancellationToken cancel = default(CancellationToken))
        {
            var pubsub = await ipfs.PubSubService.ConfigureAwait(false);
            await pubsub.PublishAsync(topic, message, cancel);
        }

        public async Task SubscribeAsync(string topic,
            Action<IPublishedMessage> handler,
            CancellationToken cancellationToken)
        {
            var pubsub = await ipfs.PubSubService.ConfigureAwait(false);
            await pubsub.SubscribeAsync(topic, handler, cancellationToken);
        }

        public async Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel =
            default(CancellationToken))
        {
            var pubsub = await ipfs.PubSubService.ConfigureAwait(false);
            return await pubsub.SubscribedTopicsAsync(cancel);
        }
    }
}
