using System;
using System.Collections.Generic;
using System.Net;
using Catalyst.Node.Common.Modules.P2P.Messages;
using Catalyst.Node.Core.Helpers.IO;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.Modules.P2P.Messages
{
    public class MessageQueueManager : IMessageSender
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //        private readonly IMessageListener _listener;
        private readonly List<IPAddress> _blackList;
        internal readonly Queue<Message> _receivedMessageQueue;
        private readonly Dictionary<IPAddress, int> _requestsByIp;
        private readonly Queue<Message> _sendMessageQueue;

        public void Send(IConnection connection, IMessage message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        public void Send(Connection connection, IMessage message)
        {
            lock (_sendMessageQueue)
            {
                //@TODO
                //                var package = new Message(endPoint, message, message.Length); 
                //                _sendMessageQueue.Enqueue(package);
            }
        }
        //        public EventHandler<PackageReceivedEventArgs<IPEndPoint>> PackageReceivedEventArgs;

        //        internal MessageQueueManager(IMessageListener listener, IWorkScheduler worker)
        //        {
        //            _listener = listener;

        //            _receivedMessageQueue = new Queue<Message>();
        //            _sendMessageQueue = new Queue<Message>();
        //            _blackList = new List<IPAddress>();
        //            _requestsByIp = new Dictionary<IPAddress, int>();

        //            worker.QueueForever(SendReceive, TimeSpan.FromMilliseconds(200));
        //            worker.QueueForever(AnalyzeRequestList, TimeSpan.FromMinutes(1));
        //start worker?
        //        }

        //        private void ProcessMessageQueue()
        //        {
        //            Log.Message("ProcessMessageQueue");
        //            lock (MessageQueueManager.ReceivedMessageQueue)
        //            {
        //                Log.Message("Messages to process: " + ReceivedMessageQueue.Count);
        //                byte[] msg = null;
        //                var receivedCount = ReceivedMessageQueue.Count;
        //                for (var i = 0; i < receivedCount; i++)
        //                {
        //                    Log.Message("processing message: " + receivedCount);
        //                    msg = ReceivedMessageQueue.Dequeue();
        //                }
        //                byte[] msgDescriptor = msg.Slice(0, 3);
        //                byte[] message = msg.Slice(3);
        //                Log.Message(BitConverter.ToString(msgDescriptor));
        //                Log.Message(BitConverter.ToString(message));
        //            }
        //            Log.Message("unlocked msg queue");
        //        }

        /// <summary>
        /// </summary>
        private void SendReceive()
        {
            SendPendingMessages();
            ReceiveAndProcessPendingMessages();
        }

        /// <summary>
        /// </summary>
        private void ReceiveAndProcessPendingMessages()
        {
            lock (_receivedMessageQueue)
            {
                var receivedCount = _receivedMessageQueue.Count;
                for (var i = 0; i < receivedCount; i++)
                {
                    var package = _receivedMessageQueue.Dequeue();
                    //                    Events.Raise(PackageReceivedEventArgs, this, new PackageReceivedEventArgs<IPEndPoint>(package.EndPoint, package.Data, package.Count));
                }
            }
        }

        /// <summary>
        /// </summary>
        private void SendPendingMessages()
        {
            lock (_sendMessageQueue)
            {
                var sendCount = _sendMessageQueue.Count;
                for (var i = 0; i < sendCount; i++)
                {
                    var pkg = _sendMessageQueue.Dequeue();
                    //                    _listener.Send(pkg);
                }
            }
        }

        /// <summary>
        /// </summary>
        private void AnalyzeRequestList()
        {
            lock (_requestsByIp)
            {
                var ips = new IPAddress[_requestsByIp.Count];
                _requestsByIp.Keys.CopyTo(ips, 0);
                foreach (var ip in ips)
                {
                    var num = _requestsByIp[ip];

                    if (num > 10 && !IsBlocked(ip))
                    {
                        BlockIp(ip);
                        return;
                    }

                    _requestsByIp[ip] = 0;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="message"></param>
        /// <param name="count"></param>
        public void Receive(IPEndPoint endPoint, byte[] message, int count)
        {
            var ip = endPoint.Address;
            if (IsBlocked(ip)) return;
            IncrementRequestByIp(ip);

            //            var package = new Message(endPoint, message, count);
            lock (_receivedMessageQueue)
            {
                //                _receivedMessageQueue.Enqueue(package);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="ip"></param>
        private void IncrementRequestByIp(IPAddress ip)
        {
            lock (_requestsByIp)
            {
                if (_requestsByIp.ContainsKey(ip))
                {
                    _requestsByIp[ip]++;
                    return;
                }

                _requestsByIp.Add(ip, 1);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private bool IsBlocked(IPAddress ip)
        {
            return _blackList.Contains(ip);
        }

        /// <summary>
        /// </summary>
        /// <param name="ip"></param>
        public void BlockIp(IPAddress ip)
        {
            Logger.Verbose("Blocking IP {0}" + ip);
            _blackList.Add(ip);
        }
    }
}