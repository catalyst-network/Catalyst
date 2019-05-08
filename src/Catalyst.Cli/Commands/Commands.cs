#region LICENSE

/**
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

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Catalyst.Cli.Options;
using Catalyst.Cli.Rpc;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Common.Shell;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using CommandLine;
using Dawn;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Catalyst.Cli.Commands
{
    /// <inheritdoc cref="ShellBase" />
    public sealed partial class Commands : ShellBase, IAdvancedShell
    {
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly ICertificateStore _certificateStore;
        private readonly IList<IRpcNodeConfig> _rpcNodeConfigs;
        private readonly INodeRpcClientFactory _nodeRpcClientFactory;
        private readonly ISocketClientRegistry<INodeRpcClient> _socketClientRegistry;
        private readonly IFileTransfer _rpcFileTransfer;
        private readonly ILogger _logger;
        private readonly IUserOutput _userOutput;

        /// <summary>
        /// </summary>
        public Commands(INodeRpcClientFactory nodeRpcClientFactory,
            IConfigurationRoot config,
            ILogger logger,
            ICertificateStore certificateStore,
            IFileTransfer rpcFileTransfer,
            IUserOutput userOutput)
        {
            _certificateStore = certificateStore;
            _nodeRpcClientFactory = nodeRpcClientFactory;
            _logger = logger;
            _socketClientRegistry = new SocketClientRegistry<INodeRpcClient>();
            _rpcNodeConfigs = NodeRpcConfig.BuildRpcNodeSettingList(config);
            _rpcFileTransfer = rpcFileTransfer;
            _peerIdentifier = BuildCliPeerId(config);
            _userOutput = userOutput;

            _userOutput.WriteLine(@"Koopa Shell Start");
        }

        /// <inheritdoc cref="ParseCommand" />
        public override bool ParseCommand(params string[] args)
        {
            Guard.Argument(args, nameof(args)).NotNull().MinCount(1).NotEmpty();

            return Parser.Default.ParseArguments<
                    GetInfoOptions,
                    GetVersionOptions,
                    GetMempoolOptions,
                    ConnectOptions,
                    SignOptions,
                    VerifyOptions,
                    PeerListOptions,
                    PeerCountOptions,
                    RemovePeerOptions,
                    PeerReputationOptions,
                    AddFileOnDfsOptions,
                    GetFileOptions>(args)
               .MapResult(
                    (GetInfoOptions opts) => GetInfoCommand(opts),
                    (GetVersionOptions opts) => GetVersionCommand(opts),
                    (GetMempoolOptions opts) => GetMempoolCommand(opts),
                    (SignOptions opts) => MessageSignCommand(opts),
                    (VerifyOptions opts) => MessageVerifyCommand(opts),
                    (PeerListOptions opts) => PeerListCommand(opts),
                    (PeerCountOptions opts) => PeerCountCommand(opts),
                    (RemovePeerOptions opts) => PeerRemoveCommand(opts),
                    (PeerReputationOptions opts) => PeerReputationCommand(opts),
                    (AddFileOnDfsOptions opts) => DfsAddFile(opts),
                    (ConnectOptions opts) => OnConnectNode(opts.NodeId),
                    (ConnectOptions opts) => DisconnectNode(opts.NodeId),
                    (GetFileOptions opts) => OnGetFileOptions(opts),
                    errs => false);
        }

        private bool OnGetFileOptions(GetFileOptions opts)
        {
            INodeRpcClient node;
            try
            {
                node = GetConnectedNode(opts.Node);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return false;
            }

            var nodeConfig = GetNodeConfig(opts.Node);
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull();

            var nodePeerIdentifier = new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey),
                nodeConfig.HostAddress, nodeConfig.Port);

            var message = new GetFileFromDfsRequest()
            {
                DfsHash = opts.FileHash
            };

            var messageDto = new RpcMessageFactory<GetFileFromDfsRequest>().GetMessage(message, nodePeerIdentifier,
                _peerIdentifier, MessageTypes.Ask);

            var fileTransfer = FileTransferInformation.BuildDownload(
                _peerIdentifier,
                new PeerIdentifier(messageDto.PeerId),
                node.Channel,
                messageDto.CorrelationId.ToGuid(),
                opts.FileOutput,
                0
            );

            fileTransfer.AddSuccessCallback(OnSuccess);
            _rpcFileTransfer.InitializeTransfer(fileTransfer);

            node.SendMessage(messageDto);

            var originalLogLevel = Program.LogLevelSwitch.MinimumLevel;

            Program.LogLevelSwitch.MinimumLevel = LogEventLevel.Error;

            while (!fileTransfer.IsComplete() && !fileTransfer.IsExpired())
            {
                _userOutput.Write("\rDownloaded: " + fileTransfer.GetPercentage() + "%");
                System.Threading.Thread.Sleep(500);
            }

            _userOutput.Write("\rDownloaded: " + fileTransfer.GetPercentage() + "%\n");

            Program.LogLevelSwitch.MinimumLevel = originalLogLevel;

            return true;
        }

        private void OnSuccess(IFileTransferInformation obj)
        {
            File.Move(obj.TempPath, obj.FileOutputPath);
        }

        public static IPeerIdentifier BuildCliPeerId(IConfiguration configuration)
        {
            //TODO: Handle different scenarios to get the IPAddress and Port depending
            //on you whether you are connecting to a local node, or a remote one.
            //https://github.com/catalyst-network/Catalyst.Node/issues/307

            return new PeerIdentifier(configuration.GetSection("CatalystCliConfig")
                   .GetSection("PublicKey").Value.ToBytesForRLPEncoding(),
                IPAddress.Loopback, IPEndPoint.MaxPort);
        }

        /// <summary>
        /// Connects a valid and configured node to the RPC server.
        /// </summary>
        /// <param name="nodeId">a string including the node ID.</param>
        /// <returns>Returns true unless an unhandled exception occurs.</returns>
        private bool OnConnectNode(string nodeId)
        {
            Guard.Argument(nodeId, nameof(nodeId)).NotEmpty();
            var rpcNodeConfigs = GetNodeConfig(nodeId);
            Guard.Argument(rpcNodeConfigs, nameof(rpcNodeConfigs)).NotNull();

            try
            {
                //Connect to the node and store it in the socket client registry
                var nodeRpcClient = _nodeRpcClientFactory.GetClient(_certificateStore.ReadOrCreateCertificateFile(rpcNodeConfigs.PfxFileName), rpcNodeConfigs);

                if (!IsSocketChannelActive(nodeRpcClient))
                {
                    return false;
                }

                var clientHashCode = _socketClientRegistry.GenerateClientHashCode(
                    EndpointBuilder.BuildNewEndPoint(rpcNodeConfigs.HostAddress, rpcNodeConfigs.Port));

                _socketClientRegistry.AddClientToRegistry(clientHashCode, nodeRpcClient);
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message, e);
                return false;
            }

            return true;
        }

        /// <inheritdoc cref="DisconnectNode" />
        private bool DisconnectNode(string nodeId)
        {
            Guard.Argument(nodeId, nameof(nodeId)).Contains(typeof(string));
            var nodeConfig = GetNodeConfig(nodeId);
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull();

            var registryId = _socketClientRegistry.GenerateClientHashCode(
                EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port));

            var node = _socketClientRegistry.GetClientFromRegistry(registryId);
            Guard.Argument(node, nameof(node)).Require(IsSocketChannelActive(node));

            node.Shutdown().GetAwaiter().OnCompleted(() => { _socketClientRegistry.RemoveClientFromRegistry(registryId); });

            return true;
        }

        /// <inheritdoc cref="GetConnectedNode" />
        public INodeRpcClient GetConnectedNode(string nodeId)
        {
            Guard.Argument(nodeId, nameof(nodeId)).NotNull().NotEmpty().Compatible<string>();
            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(node => node.NodeId.Equals(nodeId));
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull();

            var registryId = _socketClientRegistry.GenerateClientHashCode(
                EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port));

            var nodeRpcClient = _socketClientRegistry.GetClientFromRegistry(registryId);
            Guard.Argument(nodeRpcClient).Require(IsSocketChannelActive(nodeRpcClient));

            return nodeRpcClient;
        }

        /// <inheritdoc cref="GetNodeConfig" />
        private IRpcNodeConfig GetNodeConfig(string nodeId)
        {
            Guard.Argument(nodeId, nameof(nodeId)).NotNull().NotEmpty().Compatible<string>();

            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(config => config.NodeId.Equals(nodeId));

            if (nodeConfig == null)
            {
                _userOutput.WriteLine("Node not configured. Add node to config file and try again.");
                return null;
            }

            return nodeConfig;
        }

        /// <summary>
        /// Checks if the socket channel opened with the RPC server in the node is still active.
        /// </summary>
        /// <param name="node">A <see cref="IRpcNode"/> object including node required information.</param>
        /// <returns>Returns True if the channel is still active and False otherwise.  A "Channel inactive ..." message is returned to the console.</returns>
        private bool IsSocketChannelActive(INodeRpcClient node)
        {
            Guard.Argument(node, nameof(node)).Compatible<INodeRpcClient>();
            try
            {
                Guard.Argument(node.Channel.Active, nameof(node.Channel.Active)).True();
                return true;
            }
            catch (Exception e)
            {
                _logger.Information("Channel inactive ...");
                _logger.Debug(e.Message);
                return false;
            }
        }
    }
}
