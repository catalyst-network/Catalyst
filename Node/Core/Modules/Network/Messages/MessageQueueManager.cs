using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using ADL.Node.Core.Modules.Network.Workers;

namespace ADL.Node.Core.Modules.Network.Messages
{
     public class MessageQueueManager : IMessageSender
    {
//        private readonly IMessageListener _listener;
        private readonly List<IPAddress> _blackList;
        private readonly Queue<Message> _sendMessageQueue;
        internal readonly Queue<Message> _receivedMessageQueue;
        private readonly Dictionary<IPAddress, int> _requestsByIp;
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

//        private void ProcessMessageQueue()//@TODO this is duplicated in message queue manager
//        {
//            Log.Log.Message("ProcessMessageQueue");
//            lock (MessageQueueManager.ReceivedMessageQueue)
//            {
//                Log.Log.Message("Messages to process: " + ReceivedMessageQueue.Count);
//                byte[] msg = null;
//                var receivedCount = ReceivedMessageQueue.Count;
//                for (var i = 0; i < receivedCount; i++)
//                {
//                    Log.Log.Message("processing message: " + receivedCount);
//                    msg = ReceivedMessageQueue.Dequeue();
//                }
//                byte[] msgDescriptor = msg.Slice(0, 3);
//                byte[] message = msg.Slice(3);
//                Log.Log.Message(BitConverter.ToString(msgDescriptor));
//                Log.Log.Message(BitConverter.ToString(message));
//            }
//            Log.Log.Message("unlocked msg queue");
//        }

        private void SendReceive()
        {
            SendPendingMessages();
            ReceiveAndProcessPendingMessages();
        }

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

        private void AnalyzeRequestList()
        {
            lock (_requestsByIp)
            {
                var ips = new IPAddress[_requestsByIp.Count];
                _requestsByIp.Keys.CopyTo(ips, 0);
                foreach (var ip in ips)
                {
                    var num = _requestsByIp[ip];

                    if(num > 10 && !IsBlocked(ip))
                    {
                        BlockIp(ip);
                        return;
                    }
                    _requestsByIp[ip] = 0;
                }
            }
        }

        public void Send(IPEndPoint endPoint, byte[] message)
        {
            lock (_sendMessageQueue)
            {
//                var package = new Message(endPoint, message, message.Length); 
//                _sendMessageQueue.Enqueue(package);
            }
        }

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

        private void IncrementRequestByIp(IPAddress ip)
        {
            lock (_requestsByIp)
            {
                if(_requestsByIp.ContainsKey(ip))
                {
                    _requestsByIp[ip]++;
                    return;
                }
                _requestsByIp.Add(ip, 1);
            }
        }

        private bool IsBlocked(IPAddress ip)
        {
            return _blackList.Contains(ip);
        }

        public void BlockIp(IPAddress ip)
        {
            Log.Log.Message("Blocking IP {0}" + ip);
            _blackList.Add(ip);
        }
    }
}