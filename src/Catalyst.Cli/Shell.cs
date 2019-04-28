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
using System.Text;
using Catalyst.Cli.Rpc;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO;
using Catalyst.Common.Shell;
using Dawn;
using Microsoft.Extensions.Configuration;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Rpc.Node;
using CommandLine;
using ILogger = Serilog.ILogger;
using Nethereum.RLP;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.Config;
using Catalyst.Common.IO.Messaging;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.Rpc.Messaging;
using Google.Protobuf;

namespace Catalyst.Cli
{
    public sealed class Shell
        : ShellBase,
            IAdvancedShell
    {
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly ICertificateStore _certificateStore;
        private readonly IList<IRpcNodeConfig> _rpcNodeConfigs;
        private readonly INodeRpcClientFactory _nodeRpcClientFactory;
        private readonly ISocketClientRegistry<INodeRpcClient> _socketClientRegistry;

        private readonly ILogger _logger;

        private const string NoConfigMessage =
            "Node not configured. Add node to config file and try again.";

        /// <summary>
        /// </summary>
        public Shell(INodeRpcClientFactory nodeRpcClientFactory,
            IConfigurationRoot config,
            ILogger logger,
            ICertificateStore certificateStore)
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
            //TODO: Handle different scenarios to get the IPAddress and Port depending
            //on you whether you are connecting to a local node, or a remote one.
            //https://github.com/catalyst-network/Catalyst.Node/issues/307

            return new PeerIdentifier(configuration.GetSection("CatalystCliConfig")
                   .GetSection("PublicKey").Value.ToBytesForRLPEncoding(),
                IPAddress.Loopback, IPEndPoint.MaxPort);
        }

        /// <summary>
        /// Parses the Options object sent and calls the correct message to handle the option a defined in the MapResult
        /// </summary>
        /// <param name="args">string array including the parameters passed through the command line</param>
        /// <returns>Returns true if a method to handle the options is found otherwise returns false</returns>
        public override bool ParseCommand(params string[] args)
        {
            Guard.Argument(args, nameof(args)).NotNull().MinCount(1).NotEmpty();

            return Parser.Default.ParseArguments<GetInfoOptions,
                    ConnectOptions,
                    SignOptions,
                    VerifyOptions,
                    PeerListOptions>(args)
               .MapResult<GetInfoOptions, ConnectOptions, SignOptions, VerifyOptions, PeerListOptions, bool>(
                    (GetInfoOptions opts) => OnGetCommands(opts),
                    (ConnectOptions opts) => OnConnectNode(opts.NodeId),
                    (SignOptions opts) => OnSignCommands(opts),
                    (VerifyOptions opts) => OnVerifyCommands(opts),
                    (PeerListOptions opts) => OnPeerListCommands(opts),
                    errs => false);
        }

        /// <summary>
        /// Calls the specific option handler method from one of the "get" command options
        /// based on the options passed in by the user through the command line.  The available options are:
        /// 1- get config
        /// 2- get version
        /// 3- get mempool
        /// </summary>
        /// <param name="opts">An object of <see cref="GetInfoOptions"/> populated by the parser</param>
        /// <returns>Returns true if the command was correctly handled. This does not mean that the command ended successfully.
        /// Error messages returned to the user is considered a correct command handling</returns>
        private bool OnGetCommands(GetInfoOptions opts)
        {
            Guard.Argument(opts).NotNull();
            
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
        /// Calls the specific option handler method from one of the "sign" command options based on the options passed
        /// in by he user through the command line.  The available options are:
        /// 1- sign message
        /// </summary>
        /// <param name="opts">An object of <see cref="SignOptions"/> populated by the parser</param>
        /// <returns>Returns true if the command was correctly handled. This does not mean that the command ended successfully.
        /// Error messages returned to the user is considered a correct command handling</returns>
        private bool OnSignCommands(SignOptions opts)
        {
            Guard.Argument(opts).NotNull();
            
            if (opts.Message.Length > 0)
            {
                return OnSignMessage(opts);
            }

            return false;
        }
        
        /// <summary>
        /// Calls the specific option handler method from one of the "sign" command options based on the options passed
        /// in by he user through the command line.  The available options are:
        /// 1- sign message
        /// </summary>
        /// <param name="opts">An object of <see cref="SignOptions"/> populated by the parser</param>
        /// <returns>Returns true if the command was correctly handled. This does not mean that the command ended successfully.
        /// Error messages returned to the user is considered a correct command handling</returns>
        private bool OnVerifyCommands(VerifyOptions opts)
        {
            if (opts.Message.Length > 0)
            {
                return OnVerifyMessage(opts);
            }

            return false;
        }

        /// <summary>
        /// Called when [peer list commands].
        /// </summary>
        /// <param name="opts">The options.</param>
        /// <returns>[true] if correct arguments, [false] if arguments are invalid</returns>
        private bool OnPeerListCommands(PeerListOptions opts)
        {
            if (opts.Node.Length > 0)
            {
                return OnListPeerNodes(opts);
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
        /// Connects a valid and configured node to the RPC server.
        /// </summary>
        /// <param name="nodeId">a string including the node ID.</param>
        /// <returns>Returns true unless an unhandled exception occurs.</returns>
        private bool OnConnectNode(string nodeId)
        {
            Guard.Argument(nodeId).NotEmpty();
            
            var rpcNodeConfigs = GetNodeConfig(nodeId);
            
            //Check if there is a connection has already been made to the node
            Guard.Argument(rpcNodeConfigs).NotNull();
            
            try
            {
                //Connect to the node and store it in the socket client registry
                var nodeRpcClient = _nodeRpcClientFactory.GetClient(_certificateStore.ReadOrCreateCertificateFile(rpcNodeConfigs.PfxFileName), rpcNodeConfigs);
                var clientHashCode =
                    _socketClientRegistry.GenerateClientHashCode(
                        EndpointBuilder.BuildNewEndPoint(rpcNodeConfigs.HostAddress, rpcNodeConfigs.Port));
                _socketClientRegistry.AddClientToRegistry(clientHashCode, nodeRpcClient);
            }

            //Handle any other exception. This is a generic error message and should not be returned to users but added
            //as a safe fail
            catch (Exception e)
            {
                _logger.Debug(e.Message, e);
            }

            return true;
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
        /// Gets the version of a node
        /// </summary>
        /// <returns>Returns true if successful and false otherwise.</returns>
        protected override bool OnGetVersion(object opts)
        {
            Guard.Argument(opts).NotNull().Compatible<GetInfoOptions>();

            var nodeId = ((GetInfoOptions) opts).NodeId;

            var node = GetConnectedNode(nodeId);
            Guard.Argument(node).NotNull("Node cannot be null. The shell must be able to connect to a valid node to be able to send the request.");

            try
            {
                //send the message to the server by writing it to the channel
                var request = new VersionRequest();

                node.SendMessage(request.ToAnySigned(_peerIdentifier.PeerId, Guid.NewGuid()));
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
                throw;
            }

            return true;
        }

        /// <summary>
        /// Handles the command <code>get -i [node-name]</code>.  The method makes sure first the CLI is connected to
        /// the node specified in the command and then creates a <see cref="GetInfoRequest"/> object and sends it in a
        /// message to the RPC server in the node.
        /// </summary>
        /// <param name="opts"><see cref="GetInfoOptions"/> object including the options entered through the CLI.</param>
        /// <returns>True if the message is sent successfully to the node and False otherwise.</returns>
        protected override bool OnGetConfig(object opts)
        {
            Guard.Argument(opts).NotNull().Compatible<GetInfoOptions>();
            
            var nodeId = ((GetInfoOptions) opts).NodeId;

            var node = GetConnectedNode(nodeId);
            Guard.Argument(node).NotNull("Node cannot be null. The shell must be able to connect to a valid node to be able to send the request.");
            
            var nodeConfig = GetNodeConfig(nodeId);
            try
            {
                var request = new RpcMessageFactory<VersionRequest, RpcMessages>().GetMessage(
                    new MessageDto<VersionRequest, RpcMessages>(
                        RpcMessages.GetMempoolRequest,
                        new VersionRequest
                        {
                            Query = true
                        },
                        new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey), nodeConfig.HostAddress, nodeConfig.Port),
                        _peerIdentifier)
                );
                
                node.SendMessage(request);
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
        /// Handles the command <code>get -m [node-name]</code>.  The method makes sure first the CLI is connected to
        /// the node specified in the command and then creates a <see cref="GetMempoolRequest"/> object and sends it in a
        /// message to the RPC server in the node.
        /// </summary>
        /// <param name="opts"><see cref="GetInfoOptions"/> object including the options entered through the CLI.</param>
        /// <returns>True if the message is sent successfully to the node and False otherwise.</returns>
        protected override bool OnGetMempool(object opts)
        {
            Guard.Argument(opts).NotNull().Compatible<GetInfoOptions>();
            
            var nodeId = ((GetInfoOptions) opts).NodeId;

            var node = GetConnectedNode(nodeId);
            Guard.Argument(node).NotNull();

            //if the node is connected and there are no other errors then send the get info request to the server
            try
            {
                //send the message to the server by writing it to the channel
                var request = new GetMempoolRequest();

                node.SendMessage(request.ToAnySigned(_peerIdentifier.PeerId, Guid.NewGuid()));
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
                throw;
            }

            return true;
        }

        /// <summary>
        /// Sign a message with the private key of an address.
        /// </summary>
        /// <param name="opts"><see cref="GetInfoOptions"/> object including the options entered through the CLI.</param>
        /// <returns>True if the message is sent successfully to the node and False otherwise.</returns>
        public bool OnSignMessage(object opts)
        {
            Guard.Argument(opts).NotNull().Compatible<SignOptions>();

            var signOptions = (SignOptions) opts;
            var nodeId = signOptions.Node;

            //Perform validations required before a command call
            var node = GetConnectedNode(nodeId);
            Guard.Argument(node).NotNull();

            //if the node is connected and there are no other errors then send the get info request to the server
            try
            {
                //send the message to the server by writing it to the channel
                var request = new SignMessageRequest
                {
                    Message = ByteString.CopyFrom(signOptions.Message.Trim('\"'), Encoding.UTF8)
                       .ToByteString()
                };

                node.SendMessage(request.ToAnySigned(_peerIdentifier.PeerId, Guid.NewGuid())).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return true;
        }

        /// <summary>
        /// Verifies a signed message.
        /// </summary>
        /// <param name="opts"><see cref="GetInfoOptions"/> object including the options entered through the CLI.</param>
        /// <returns>True if the message is sent successfully to the node and False otherwise.</returns>
        public override bool OnVerifyMessage(object opts)
        {
            Guard.Argument(opts).NotNull().Compatible<VerifyOptions>();
            
            //get the message to verify, the address/public key who signed it, and the signature 
            var verifyOptions = (VerifyOptions) opts;
            
            //if the node is connected and there are no other errors then send the get info request to the server
            try
            {
                var node = GetConnectedNode(verifyOptions.Node);
                Guard.Argument(node).NotNull();

                //create and populate a VerifyMessage request
                var request = new VerifyMessageRequest
                {
                    Message =
                        RLP.EncodeElement(verifyOptions.Message.Trim('\"').ToBytesForRLPEncoding()).ToByteString(),
                    PublicKey = verifyOptions.Address.ToBytesForRLPEncoding().ToByteString(),
                    Signature = verifyOptions.Signature.ToBytesForRLPEncoding().ToByteString()
                };

                //send the message to the server for handling by writing it to the channel
                node.SendMessage(request.ToAnySigned(_peerIdentifier.PeerId, Guid.NewGuid())).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
        /// Checks if the socket channel opened with the RPC server in the node is still active.
        /// </summary>
        /// <param name="node">A <see cref="IRpcNode"/> object including node required information.</param>
        /// <returns>Returns True if the channel is still active and False otherwise.  A "Channel inactive ..." message is returned to the console.</returns>
        public bool IsSocketChannelActive(INodeRpcClient node)
        {
            if (node.Channel.Active)
            {
                return true;
            }

            _logger.Information("Channel inactive ...");
            return false;
        }

        public INodeRpcClient GetConnectedNode(string nodeId)
        {
            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(node => node.NodeId.Equals(nodeId));

            Guard.Argument(nodeConfig).NotNull();

            var registryId = _socketClientRegistry.GenerateClientHashCode(
                EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port));
            return _socketClientRegistry.GetClientFromRegistry(registryId);
        }

        private IRpcNodeConfig GetNodeConfig(string nodeId)
        {
            var config = _rpcNodeConfigs.SingleOrDefault(nodeConfig => nodeConfig.NodeId.Equals(nodeId));

            if (config != null)
            {
                return config;
            }

            ReturnUserMessage(NoConfigMessage);
            return null;
        }

        private void ReturnUserMessage(string message)
        {
            Console.WriteLine(message);
        }

        /// <inheritdoc />
        /// <summary>
        /// Called when [list peer nodes].
        /// </summary>
        /// <param name="opts">The arguments.</param>
        /// <returns>True if command was successful</returns>
        protected override bool OnListPeerNodes(object opts)
        {
            try
            {
                Guard.Argument(opts).NotNull().Compatible<PeerListOptions>();

                var peerListOptions = (PeerListOptions) opts;
                var node = GetConnectedNode(peerListOptions.Node);
                var nodeConfig = GetNodeConfig(peerListOptions.Node);

                Guard.Argument(node).NotNull();

                var rpcMessageFactory = new RpcMessageFactory<GetPeerListRequest, RpcMessages>();
                var request = new GetPeerListRequest();

                var requestMessage = rpcMessageFactory.GetMessage(new MessageDto<GetPeerListRequest, RpcMessages>
                (
                    type: RpcMessages.GetPeerListRequest,
                    message: request,
                    recipient: new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey), nodeConfig.HostAddress, nodeConfig.Port),
                    sender: _peerIdentifier
                ));

                node.SendMessage(requestMessage).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            return true;
        }
    }
}
