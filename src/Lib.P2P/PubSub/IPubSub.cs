using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lib.P2P.PubSub
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPubSub
    {
        /// <summary>
        ///   Get the subscribed topics.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a sequence of <see cref="string"/> for each topic.
        /// </returns>
        Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Get the peers that are pubsubing with us.
        /// </summary>
        /// <param name="topic">
        ///   When specified, only peers subscribing on the topic are returned.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a sequence of <see cref="Peer"/>.
        /// </returns>
        Task<IEnumerable<Peer>> PeersAsync(string topic = null, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Publish a string message to a given topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PublishAsync(string topic, string message, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Publish a binary message to a given topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PublishAsync(string topic, byte[] message, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Publish a binary message to a given topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PublishAsync(string topic, Stream message, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Subscribe to messages on a given topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <param name="handler">
        ///   The action to perform when a <see cref="IPublishedMessage"/> is received.
        /// </param>
        /// <param name="cancellationToken">
        ///   Is used to stop the topic listener.  When cancelled, the <see cref="OperationCanceledException"/>
        ///   is <b>NOT</b> raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   The <paramref name="handler"/> is invoked on the topic listener thread.
        /// </remarks>
        Task SubscribeAsync(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken);
    }
}
