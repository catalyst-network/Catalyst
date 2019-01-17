using System;
using System.Net;
using System.Collections.Generic;
using Catalyst.Helpers.Logger;
using Catalyst.Node.Modules.Core.P2P.Connections;
using Catalyst.Node.Modules.Core.P2P.Peer;
using Catalyst.Helpers.Util;

namespace Catalyst.Node.Modules.Core.P2P.Messages
{
    class MessageReplyWaitManager 
    {
        private readonly PeerList _peerList;
        private readonly IMessageSender _messageSender;
        private readonly Dictionary<ulong, MessageReplyWait> _internal = new Dictionary<ulong, MessageReplyWait>();
        
        private static readonly object LockObject = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageSender"></param>
        /// <param name="peerList"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MessageReplyWaitManager(IMessageSender messageSender, PeerList peerList)
        {
            //@TODO guard util
            if (peerList == null) throw new ArgumentNullException(nameof(peerList));
            if (messageSender == null) throw new ArgumentNullException(nameof(messageSender));
            
            _peerList = peerList;
            _messageSender = messageSender;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Add(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            var correlationId = ByteUtil.GenerateCorrelationId();
            
            lock (LockObject)
            {
                if(!_internal.ContainsKey(correlationId))
                {
                    _internal.Add(correlationId, new MessageReplyWait(message, correlationId));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool VerifyExpected(IPEndPoint endPoint, ulong correlationId)
        {
            if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));
            if (correlationId <= 0) throw new ArgumentOutOfRangeException(nameof(correlationId));
            
            lock (LockObject)
            {
                if (_internal.ContainsKey(correlationId))
                {
                    if (Equals(_internal[correlationId].Message.Connection.EndPoint, endPoint))
                    {
                        _internal.Remove(correlationId);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void PurgeTimeouts()
        {
            lock (LockObject)
            {
                var array = new MessageReplyWait[_internal.Count];
                _internal.Values.CopyTo(array, 0);
                foreach (var replyWait in array)
                {
                    if (!replyWait.IsTimeout)
                    {
                        Log.Message($"Timeout message attempt {{0}} for {{1}} sent to {{2}} correlation {{3}} {replyWait.Attempts} {replyWait.Message.Connection.EndPoint} {replyWait.Sent.ToLocalTime()} {replyWait.CorrelationId}");
                        continue;
                    }

                    if (replyWait.Attempts++ >= 3)
                    {
                        _internal.Remove(replyWait.CorrelationId);
                        if (_peerList.SearchLists(replyWait.Message.Connection, out Connection connection))
                        {
                            _peerList.Punish(connection.Peer);
                        }
                    }
                    else
                    {
                        replyWait.Sent = DateTimeProvider.UtcNow;
                        Log.Message($"Retrying message attempt {{0}} for {{1}} sent to {{2}} correlation {{3}} {replyWait.Attempts} {replyWait.Message.Connection.EndPoint} {replyWait.Sent.ToLocalTime()} {replyWait.CorrelationId}");
                        _messageSender.Send(replyWait.Message.Connection, replyWait.Message.ProtoMessage);
                    }
                }
            }
        }
    }
}
