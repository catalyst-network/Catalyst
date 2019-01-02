using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace ADL.Node.Core.Modules.Network.Messages
{
    class MessageReplyWaitManager 
    {
//        private readonly IMessageSender _messageSender;
//        private readonly PeerList _peerList;
//        private readonly Dictionary<ulong, ReplyWait> _internal = new Dictionary<ulong, ReplyWait>();
//        private static readonly object LockObject = new object();

//        public ReplyWaitManager(IMessageSender messageSender, PeerList peerList)
//        {
//            _messageSender = messageSender;
//            _peerList = peerList;
//        }

//        public void Add(Package package, ulong correlationId)
//        {
//            lock (LockObject)
//            {
//                if(!_internal.ContainsKey(correlationId))
//                {
//                    _internal.Add(correlationId, new ReplyWait(package, correlationId));
//                }
//            }
//        }

//        public bool VerifyExpected(IPEndPoint endPoint, ulong correlationId)
//        {
//            lock (LockObject)
//            {
//                if (_internal.ContainsKey(correlationId))
//                {
//                    if (Equals(_internal[correlationId].Package.EndPoint, endPoint))
//                    {
//                        _internal.Remove(correlationId);
//                        return true;
//                    }
//                }
//            }
//            return false;
//        }


//        public void PurgeTimeouts()
//        {
//            lock (LockObject)
//            {
//                var array = new ReplyWait[_internal.Count];
//                _internal.Values.CopyTo(array, 0);
//                foreach (var replyWait in array)
//                {
//                    if (!replyWait.IsTimeout)
//                    {
//                        Logger.Verbose("Timeout message attempt {0} for {1} sent to {2} correlation {3}", replyWait.Attempts, replyWait.Package.EndPoint, replyWait.Sent.ToLocalTime(), replyWait.CorrelationId);
//                        continue;
//                    }

//                    if (replyWait.Attempts++ >= 3)
//                    {
//                        _internal.Remove(replyWait.CorrelationId);
//                        PeerInfo peerInfo;
//                        if (_peerList.TryGet(replyWait.Package.EndPoint, out peerInfo))
//                        {
//                            _peerList.Punish(peerInfo.BotId);
//                        }
//                    }
//                    else
//                    {
//                        replyWait.Sent = DateTimeProvider.UtcNow;
//                        Logger.Verbose("Retrying message attempt {0} for {1} sent to {2} correlation {3}", replyWait.Attempts, replyWait.Package.EndPoint, replyWait.Sent.ToLocalTime(), replyWait.CorrelationId);
//                        _messageSender.Send(replyWait.Package.EndPoint, replyWait.Package.Data);
//                    }
//                }
//            }
//        }
    }
}