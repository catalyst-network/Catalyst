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
using System.Linq;
using System.Net;
using Catalyst.Cli.Rpc;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.Shell;
using Catalyst.Node.Common.Interfaces;
using Dawn;
using Microsoft.Extensions.Configuration;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using CommandLine;
using ILogger = Serilog.ILogger;
using DotNetty.Transport.Channels;
using Nethereum.RLP;

namespace Catalyst.Cli
{
    public sealed class Shell : ShellBase, IAds, IObserver<IChanneledMessage<AnySigned>>
    {
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly ICertificateStore _certificateStore;
        private readonly IList<IRpcNodeConfig> _rpcNodeConfigs;
        private readonly INodeRpcClientFactory _nodeRpcClientFactory;
        private readonly ISocketClientRegistry<INodeRpcClient> _socketClientRegistry;

        private IChanneledMessage<AnySigned> Response { get; set; }
        private readonly ILogger _logger;

        private const string NoConfigMessage =
            "Node not configured. Add node to config file and try again.";

        private const string NodeConnectedMessage = "Connection already established with the node.";
        private const string NodeNotConnectedMessage = "Node is not connected. Connect to node first.";
        private const string ChannelInactiveMessage = "Node is not connected. Connect to node first.";

        /// <summary>
        /// </summary>
        public Shell(INodeRpcClientFactory nodeRpcClientFactory, IConfigurationRoot config, ILogger logger, ICertificateStore certificateStore)
        {
            _certificateStore = certificateStore;
            _nodeRpcClientFactory = nodeRpcClientFactory;
            _socketClientRegistry = new SocketClientRegistry<INodeRpcClient>();
            _rpcNodeConfigs = NodeRpcConfig.BuildRpcNodeSettingList(config);
            _logger = logger;
            _peerIdentifier = BuildCliPeerId(config);

            Console.WriteLine(@"Koopa Shell Start");
        }

        private static IPeerIdentifier BuildCliPeerId(IConfiguration configuration)
        {
            return new PeerIdentifier(configuration.GetSection("CatalystCliConfig")
                   .GetSection("PublicKey").Value.ToBytesForRLPEncoding(),
                IPAddress.Loopback, IPEndPoint.MaxPort
            );
        }

        public override bool ParseCommand(params string[] args)
        {
            return Parser.Default.ParseArguments<GetInfoOptions, ConnectOptions>(args)
               .MapResult(
                    (GetInfoOptions opts) => OnGetCommands(opts),
                    (ConnectOptions opts) => OnConnectNode(opts),
                    errs => false);
        }

        private bool OnGetCommands(GetInfoOptions opts)
        {
            if (opts.Info)
            {
                return OnGetConfig(opts);
            }

            if (opts.Mempool)
            {
                return OnGetMempool(opts);
            }

            if (opts.Version)
            {
                return OnGetVersion(opts);
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnDfsCommand(string[] args)
        {
            switch (args[2].ToLower(AppCulture))
            {
                case "start":
                    throw new NotImplementedException();
                case "stop":
                    throw new NotImplementedException();
                case "status":
                    throw new NotImplementedException();
                case "restart":
                    throw new NotImplementedException();
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnWalletCommand(string[] args)
        {
            switch (args[2].ToLower(AppCulture))
            {
                case "start":
                case "stop":
                case "status":
                case "restart":
                    throw new NotImplementedException();
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnPeerCommand(string[] args)
        {
            switch (args[2].ToLower(AppCulture))
            {
                case "start":
                case "stop":
                case "status":
                case "restart":
                    throw new NotImplementedException();
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnConsensusCommand(string[] args)
        {
            switch (args[2].ToLower(AppCulture))
            {
                case "start":
                    throw new NotImplementedException();
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private bool OnHelpCommand()
        {
            var advancedCmds =
                "Advanced Commands:\n" +
                "\tconnect node\n" +
                "\tget delta\n" +
                "\tget mempool\n" +
                "\tregenerate cert\n" +
                "\tmessage sign\n" +
                "\tmessage verify\n" +
                "Dfs Commands:\n" +
                "\tdfs file put\n" +
                "\tdfs file get\n" +
                "Wallet Commands:\n" +
                "\twallet create\n" +
                "\twallet list\n" +
                "\twallet export\n" +
                "\twallet balance\n" +
                "\twallet addresses create\n" +
                "\twallet addresses get\n" +
                "\twallet addresses list\n" +
                "\twallet addresses validate\n" +
                "\twallet privatekey import\n" +
                "\twallet privatekey export\n" +
                "\twallet transaction create\n" +
                "\twallet transaction sign\n" +
                "\twallet transaction decode \n" +
                "\twallet send to\n" +
                "\twallet send to from\n" +
                "\twallet send many\n" +
                "\twallet send many from\n" +
                "Peer Commands:\n" +
                "\tpeer node add\n" +
                "\tpeer node remove\n" +
                "\tpeer node blacklist\n" +
                "\tpeer node check health\n" +
                "\tpeer node request\n" +
                "\tpeer node list\n" +
                "\tpeer node info\n" +
                "\tpeer node count\n" +
                "Consensus Commands:\n" +
                "\tvote fee transaction\n" +
                "\tvote fee dfs\n" +
                "\tvote fee contract\n";
            return base.OnHelpCommand(advancedCmds);
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool OnCommand(params string[] args)
        {
            switch (args[0].ToLower(AppCulture))
            {
                case "connect":
                    return OnConnectNode(args.Skip(2).ToList());
                case "start":
                    return OnStart(args);
                case "help":
                    return OnHelpCommand();
                case "message":
                    return OnMessageCommand(args);
                case "dfs":
                    return OnDfsCommand(args);
                case "wallet":
                    return OnWalletCommand(args);
                case "peer":
                    return OnPeerCommand(args);
                case "consensus":
                    return OnConsensusCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private bool OnConnectNode(Object opts)
        {
            var nodeId = ((ConnectOptions) opts).NodeId;

            //if the node is invalid then do not continue
            if (!IsConfiguredNode(nodeId))
            {
                ReturnUserMessage(NoConfigMessage);
                return false;
            }

            var nodeConfig = GetNodeConfig(nodeId);

            //Check if there is a connection has already been made to the node
            if (IsConnectedNode(nodeId))
            {
                ReturnUserMessage(NodeConnectedMessage);
                return false;
            }

            try
            {
                //Connect to the node and store it in the socket client registry
                var nodeRpcClient = _nodeRpcClientFactory.GetClient(_certificateStore.ReadOrCreateCertificateFile(nodeConfig.PfxFileName), nodeConfig);
                var clientHashCode =
                    _socketClientRegistry.GenerateClientHashCode(
                        EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port));
                _socketClientRegistry.AddClientToRegistry(clientHashCode, nodeRpcClient);
            }

            //Handle the exception of a wrong SSL certificate password
            catch (PlatformNotSupportedException)
            {
                ReturnUserMessage(
                    $"SSL certificate {nodeConfig.PfxFileName} is invalid. Please provide a valid SSL certificate to be able to connect to the node.");
            }

            //Handle the exception of not being able to connect to the node
            catch (ConnectException)
            {
                ReturnUserMessage(
                    $"A connection to {nodeConfig.NodeId} was refused.  Please check the node status and try again.");
            }

            //Handle the exception of the connection timing out
            catch (ConnectTimeoutException)
            {
                ReturnUserMessage(
                    $"Connection to {nodeConfig.NodeId} @ {nodeConfig.HostAddress}:{nodeConfig.Port} timed out.  Please check the node status and try again.");
            }

            //Handle any other exception. This is a generic error message and should not be returned to users but added
            //as a safe fail
            catch (Exception)
            {
                ReturnUserMessage("Connection with the server couldn't be established.");
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStart(string[] args)
        {
            switch (args[1].ToLower(AppCulture))
            {
                case "work":
                    return OnStartWork(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStartWork(string[] args)
        {
            Guard.Argument(args).Contains(typeof(string));
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        public override bool OnStop(string[] args)
        {
            Guard.Argument(args).Contains(typeof(string));
            switch (args[1].ToLower(AppCulture))
            {
                case "node":
                    return OnStopNode(args);
                case "work":
                    return OnStopWork(args);
                default:
                    return true;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStopNode(string[] args)
        {
            Guard.Argument(args).Contains(typeof(string));

            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(n => n.NodeId == args[0]);

            Guard.Argument(nodeConfig).NotNull();

            var registryId =
                _socketClientRegistry.GenerateClientHashCode(
                    EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port));
            var node = _socketClientRegistry.GetClientFromRegistry(registryId);

            node.Shutdown().GetAwaiter().OnCompleted(() => { _socketClientRegistry.RemoveClientFromRegistry(registryId); });
            return true;
        }

        public void SocketClientDisconnectedHandler()
        {
            //TODO : when a connection closes unexpectedly, remove the corresponding RpcNode from _nodes list.
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStopWork(string[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnGetCommand(params string[] args)
        {
            Guard.Argument(args, nameof(args)).NotNull().MinCount(2);
            switch (args[1].ToLower(AppCulture))
            {
                case "delta":
                    return OnGetDelta(args);
                case "mempool":
                    return OnGetMempool(args.Skip(2).ToList());
                case "version":
                    return OnGetVersion(args);
                default:
                    return true;
            }
        }

        /// <summary>
        /// Gets the version of a node
        /// </summary>
        /// <returns>Returns true if successful and false otherwise.</returns>
        protected override bool OnGetVersion(Object opts)
        {
            var nodeId = ((GetInfoOptions) opts).NodeId;

            //Perform validations required before a command call
            Guard.Argument(ValidatePreCommand(nodeId)).True();

            try
            {
                var connectedNode = GetConnectedNode(nodeId);

                //send the message to the server by writing it to the channel
                var request = new VersionRequest();

                connectedNode.SendMessage(request.ToAnySigned(_peerIdentifier.PeerId, Guid.NewGuid()));
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
                throw;
            }

            return true;
        }

        protected override bool OnGetConfig(Object opts)
        {
            var nodeId = ((GetInfoOptions) opts).NodeId;

            //Perform validations required before a command call
            Guard.Argument(ValidatePreCommand(nodeId)).True();

            try
            {
                var connectedNode = GetConnectedNode(nodeId);

                //send the message to the server by writing it to the channel
                var request = new GetInfoRequest();

                connectedNode.SendMessage(request.ToAnySigned(_peerIdentifier.PeerId, Guid.NewGuid()));
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
                throw;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool OnGetDelta(string[] args)
        {
            Guard.Argument(args).Contains(typeof(string));
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get stats about the underlying mempool implementation
        /// </summary>
        /// <returns>Boolean</returns>
        protected override bool OnGetMempool(Object args)
        {
            var nodeId = ((GetInfoOptions) args).NodeId;

            try
            {
                var connectedNode = GetConnectedNode(nodeId);

                //send the message to the server by writing it to the channel
                var request = new GetMempoolRequest();

                connectedNode.SendMessage(request.ToAnySigned(_peerIdentifier.PeerId, Guid.NewGuid()));
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
                throw;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool OnMessageCommand(string[] args)
        {
            Guard.Argument(args).Contains(typeof(string));
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if the node is configured in the nodes.json config file before performing other operations.
        /// </summary>
        /// <param name="nodeId">The name of the node as entered at the command line</param>
        /// <returns>True if the node is existing in the configuration file and False otherwise</returns>
        private bool IsConfiguredNode(string nodeId)
        {
            Guard.Argument(nodeId).NotNull();

            return GetNodeConfig(nodeId) != null;
        }

        /// <summary>
        /// Checks if the node exists in the list of connected nodes.
        /// </summary>
        /// <param name="nodeId">The name of the node as entered at the command line</param>
        /// <returns>True if the node is existing in the connected nodes list and False otherwise</returns>
        public bool IsConnectedNode(string nodeId) // todo call this not go directly to GetConnectedNode() ????????
        {
            Guard.Argument(nodeId).NotNull();

            //if the node is in the list of connected nodes then a connection has already been established to it
            return (GetConnectedNode(nodeId) != null);
        }

        public bool IsSocketChannelActive(INodeRpcClient node)
        {
            if (node.Channel.Active)
            {
                return true;
            }

            _logger.Information("Channel inactive ...");
            return false;
        }

        private bool ValidatePreCommand(string nodeId)
        {
            //if the node is invalid then do not continue
            if (!IsConfiguredNode(nodeId))
            {
                ReturnUserMessage(NoConfigMessage);
                return false;
            }

            //Check if the node is already connected otherwise do not continue
            //if the node is already connected the method will return the instance
            if (!IsConnectedNode(nodeId))
            {
                ReturnUserMessage(NodeNotConnectedMessage);
                return true;
            }

            var connectedNode = GetConnectedNode(nodeId);

            //Check if the channel is still active
            if (!IsSocketChannelActive(connectedNode))
            {
                ReturnUserMessage(ChannelInactiveMessage);
                return false;
            }

            return true;
        }

        public INodeRpcClient GetConnectedNode(string nodeId)
        {
            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(node => node.NodeId.Equals(nodeId));

            Guard.Argument(nodeConfig).NotNull();

            var registryId =
                _socketClientRegistry.GenerateClientHashCode(
                    EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port));
            return _socketClientRegistry.GetClientFromRegistry(registryId);
        }

        public IRpcNodeConfig GetNodeConfig(string nodeId)
        {
            return _rpcNodeConfigs.SingleOrDefault(nodeConfig => nodeConfig.NodeId.Equals(nodeId));
        }

        private void ReturnUserMessage(string message)
        {
            Console.WriteLine(message);
        }

        /* Implementing IObserver */
        public void OnCompleted()
        {
            //Do nothing because this method should include logic to do after the observer
            //receives a message and handles it.
        }

        public void OnError(Exception error) { _logger.Error($"RpcClient observer received error : {error.Message}"); }

        public void OnNext(IChanneledMessage<AnySigned> value)
        {
            if (value == null)
            {
                return;
            }

            Response = value;
        }
    }
}
