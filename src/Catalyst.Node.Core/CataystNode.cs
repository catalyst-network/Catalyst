using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;
using Catalyst.Node.Core.Events;
using Catalyst.Node.Core.Helpers.Cryptography;
using Catalyst.Node.Core.Helpers.Logger;
using Catalyst.Node.Core.Helpers.Network;
using Catalyst.Node.Core.Helpers.Util;
using Catalyst.Node.Core.Helpers.Workers;
using Catalyst.Node.Core.Modules.P2P;
using Catalyst.Node.Core.Modules.P2P.Messages;
using Catalyst.Node.Core.P2P;
using Dawn;
using DnsClient.Protocol;
using Dns = Catalyst.Node.Core.Helpers.Network.Dns;

namespace Catalyst.Node.Core
{
    public class CatalystNode : IDisposable, IP2P
    {
        private static readonly object Mutex = new object();

        public readonly Kernel Kernel;

        /// <summary>
        ///     Instantiates basic CatalystSystem.
        /// </summary>
        private CatalystNode(Kernel kernel)
        {
            Kernel = kernel;
            SeedNodes = new List<IPEndPoint>();
            PeerManager = new PeerManager(
                Ssl.LoadCert(Kernel.NodeOptions.PeerSettings.SslCertPassword, Kernel.NodeOptions.DataDir,
                    Kernel.NodeOptions.PeerSettings.PfxFileName),
                new PeerList(new ClientWorker()),
                new MessageQueueManager(),
                Kernel.NodeIdentity
            );

            //            var server = new ServerBuilder()
            //                .UseTcp(kernel.NodeOptions.PeerSettings.Port)
            //                .SetMaximumConnections(kernel.NodeOptions.PeerSettings.MaxConnections)
            //                .UseUdp(kernel.NodeOptions.PeerSettings.Port)
            //                .RegisterPacketHandlerModule<DefaultPacketHandlerModule>()
            //                .UseProtobufNet()
            //                .Build();
            //            server.Start();

            Task.Run(async () =>
                         await PeerManager.InboundConnectionListener(
                             new IPEndPoint(Kernel.NodeOptions.PeerSettings.BindAddress,
                                 Kernel.NodeOptions.PeerSettings.Port
                             )
                         )
            );

            PeerManager.AnnounceNode += Announce;
        }

        private static CatalystNode Instance { get; set; }
        private List<IPEndPoint> SeedNodes { get; }
        private PeerManager PeerManager { get; }
        private bool Disposed { get; set; }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            Log.Message("disposing catalyst node");
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IP2P.Ping(IPeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     @TODO just to satisfy the DHT interface, need to implement
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IP2P.Store(string k, byte[] v)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        dynamic IP2P.FindValue(string k)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     If a corresponding value is present on the queried node, the associated data is returned.
        ///     Otherwise the return value is the return equivalent to FindNode()
        /// </summary>
        /// <param name="k"></param>
        /// <param name="queryingNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        List<IPeerIdentifier> IP2P.FindNode(IPeerIdentifier queryingNode, IPeerIdentifier targetNode)
        {
            // @TODO just to satisfy the DHT interface, need to implement
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        List<IPeerIdentifier> IP2P.GetPeers(IPeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="queryingNode"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        List<IPeerIdentifier> IP2P.PeerExchange(IPeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="seedServers"></param>
        internal void GetSeedNodes(List<string> seedServers)
        {
            var dnsQueryAnswers = Dns.GetTxtRecords(seedServers);
            foreach (var dnsQueryAnswer in dnsQueryAnswers)
            {
                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();
                if (answerSection != null)
                    SeedNodes.Add(EndpointBuilder.BuildNewEndPoint(answerSection.EscapedText.FirstOrDefault()));
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Announce(object sender, AnnounceNodeEventArgs e)
        {
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(e, nameof(e)).NotNull();
            var client = new TcpClient(Kernel.NodeOptions.PeerSettings.AnnounceServer.Address.ToString(),
                Kernel.NodeOptions.PeerSettings.AnnounceServer.Port);
            var nwStream = client.GetStream();
            var network = new byte[1];
            network[0] = 0x01;
            Log.ByteArr(network);
            var announcePackage = ByteUtil.Merge(network, Kernel.NodeIdentity.Id);
            Log.ByteArr(announcePackage);
            nwStream.Write(announcePackage, 0, announcePackage.Length);
            client.Close();
        }

        /// <summary>
        ///     Get a thread safe CatalystSystem singleton.
        /// </summary>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static CatalystNode GetInstance(Kernel kernel)
        {
            Guard.Argument(kernel, nameof(kernel)).NotNull();
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new CatalystNode(kernel);
                }

            return Instance;
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing) Kernel?.Dispose();

            Disposed = true;
            Log.Message("CatalystNode disposed");
        }
    }
}