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
    public interface IPubSubService : IService, IPubSub
    {
        /// <summary>
        ///   The local peer.
        /// </summary>
        Peer LocalPeer { get; set; }

        /// <summary>
        ///   Sends and receives messages to other peers.
        /// </summary>
        List<IMessageRouter> Routers { get; set; }

        /// <summary>
        ///   Creates a message for the topic and data.
        /// </summary>
        /// <param name="topic">
        ///   The topic name/id.
        /// </param>
        /// <param name="data">
        ///   The payload of message.
        /// </param>
        /// <returns>
        ///   A unique published message.
        /// </returns>
        /// <remarks>
        ///   The <see cref="PublishedMessage.SequenceNumber"/> is a monitonically 
        ///   increasing unsigned long.
        /// </remarks>
        PublishedMessage CreateMessage(string topic, byte[] data);
    }
}
