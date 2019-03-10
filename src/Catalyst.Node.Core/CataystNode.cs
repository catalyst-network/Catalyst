/*
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

using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.FileSystem;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.Consensus;
using Catalyst.Node.Common.Interfaces.Modules.Contract;
using Catalyst.Node.Common.Interfaces.Modules.Dfs;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.Interfaces.Modules.Ledger;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Core.Events;
using Catalyst.Node.Core.P2P.IO;
using Dawn;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using Serilog;

namespace Catalyst.Node.Core
{
    public class CatalystNode : IDisposable, ICatalystNode
    {
        private readonly IConsensus _consensus;
        private readonly IContract _contract;
        private readonly IDfs _dfs;
        private readonly ILedger _ledger;
        private readonly IKeySigner _keySigner;
        private readonly ILogger _logger;
        private readonly IMempool _mempool;
        private readonly IP2P _p2P;
        private IInboundTcpServer _inboundTcpServer;

        private bool _disposed;
        private readonly FileSystem _fileSystem;
        public CancellationTokenSource Ctx { get; set; }
        
        public CatalystNode(
            IP2P p2P,
            ICertificateStore certificateStore,
            IConsensus consensus,
            IDfs dfs,
            ILedger ledger,
            IKeySigner keySigner,
            ILogger logger,
            IMempool mempool = null,
            IContract contract = null
        )
        {
            _p2P = p2P;
            _consensus = consensus;
            _dfs = dfs;
            _ledger = ledger;
            _keySigner = keySigner;
            _logger = logger;
            _mempool = mempool;
            _contract = contract;
            _fileSystem = new FileSystem();
        }

        public async Task Start(CancellationTokenSource ctx)
        {
            Ctx = ctx;          
            var tlsCertificate = new X509Certificate(Path.Combine(_fileSystem.GetCatalystHomeDir().ToString(), _p2P.Settings.PfxFileName), _p2P.Settings.SslCertPassword);

            try
            {
                _inboundTcpServer = new InboundTcpServer(_p2P.Settings.BindAddress, _p2P.Settings.Port, new PeerSession(_logger));
                await _inboundTcpServer.StartAsync(tlsCertificate).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                _inboundTcpServer.Dispose();
            }
            finally
            {
                Task.WaitAll(_inboundTcpServer.StopAsync());
            }
            
            while (!Ctx.IsCancellationRequested)

            await _inboundTcpServer.StopAsync().ConfigureAwait(false);
        }
        
        /// <summary>
        /// </summary>
        /// <returns></returns>
        private void Announce(object sender, AnnounceNodeEventArgs e)
        {
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(e, nameof(e)).NotNull();
            var client = new TcpClient(_p2P.Settings.AnnounceServer.Address.ToString(),
                _p2P.Settings.AnnounceServer.Port);
            var nwStream = client.GetStream();
            var network = new byte[1];
            network[0] = 0x01;
            _logger.Debug(string.Join(" ", network));
            var announcePackage = ByteUtil.Merge(network, _p2P.Identifier.Id);
            _logger.Debug(string.Join(" ", announcePackage));
            nwStream.Write(announcePackage, 0, announcePackage.Length);
            client.Close();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                Console.WriteLine("Disposing of CatalystNode");
                _disposed = true;
                Console.WriteLine("CatalystNode disposed");
            }
        }
    }
}