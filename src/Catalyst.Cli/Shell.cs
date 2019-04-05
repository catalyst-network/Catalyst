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
using System.Diagnostics;
using System.Linq;
using System.Net;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Shell;
using Catalyst.Node.Common.Interfaces;
using Dawn;
using Microsoft.Extensions.Configuration;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using CommandLine;
using ILogger = Serilog.ILogger;
using Google.Protobuf.WellKnownTypes;
using DotNetty.Transport.Channels;

namespace Catalyst.Cli
{
    public sealed class Shell : ShellBase, IAds, IObserver<IChanneledMessage<Any>>
    {
        private readonly List<IRpcNodeConfig> _rpcNodeConfigs;
        private readonly List<IRpcNode> _nodes;

        private readonly IRpcClient _rpcClient;

        public IChanneledMessage<Any> Response { get; set; }
        private readonly ILogger _logger;

        private const string NO_CONFIG_MESSAGE =
            "Node not configured.  Add node to config file and try again.";

        private const string NODE_CONNECTED_MESSAGE = "Connection already established with the node.";
        private const string NODE_NOT_CONNECTED_MESSAGE = "Node is not connected.  Connect to node first.";
        private const string CHANNEL_INACTIVE_MESSAGE = "Node is not connected.  Connect to node first.";

        /// <summary>
        /// </summary>
        public Shell(IRpcClient rpcClient, IConfigurationRoot config, ILogger logger)
        {
            _rpcNodeConfigs = BuildRpcNodeSettingList(config);
            _rpcClient = rpcClient;
            _logger = logger;
            _nodes = new List<IRpcNode>();
            _rpcClient.MessageStream.Subscribe(this);

            Console.WriteLine(@"Koopa Shell Start");

        }
        private static List<IRpcNodeConfig> BuildRpcNodeSettingList(IConfigurationRoot config)
        {
            var section = config.GetSection("CatalystCliRpcNodes").GetSection("nodes");

            var nodeList = section.GetChildren().Select(child => new RpcNodeConfig
            {
                NodeId = child.GetSection("nodeId").Value,
                HostAddress = IPAddress.Parse(child.GetSection("host").Value),
                Port = int.Parse(child.GetSection("port").Value),
                PfxFileName = child.GetSection("PfxFileName").Value,
                SslCertPassword = child.GetSection("SslCertPassword").Value
            } as IRpcNodeConfig).ToList();

            return nodeList;
        }

        public override bool ParseCommand(params string[] args)
        {
            return Parser.Default.ParseArguments<GetInfoOptions, ConnectOptions>(args)
               .MapResult<GetInfoOptions, ConnectOptions, bool>(
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
            var nodeId = ((ConnectOptions)opts).NodeId;

            //if the node is invalid then do not continue
            if (!IsConfiguredNode(nodeId))
            {
                ReturnUserMessage(NO_CONFIG_MESSAGE);
                return true;
            }

            var nodeConfig = GetNodeConfig(nodeId);

            //Check if there is a connection has already been made to the node
            if (IsConnectedNode(nodeId))
            {
                ReturnUserMessage(NODE_CONNECTED_MESSAGE);
                return true;
            }

            try
            {
                //Connect to the node
                var socket = _rpcClient.GetClientSocketAsync(nodeConfig).GetAwaiter().GetResult();

                //if a socket could be opened with the node
                //then create IRpcNode and add it the node to the list of connected nodes
                if (socket != null)
                {
                    IRpcNode connectedNode = new RpcNode(nodeConfig, socket);
                    _nodes.Add(connectedNode);
                }

            }
            //Handle the exception of a wrong SSL certificate password
            catch (System.PlatformNotSupportedException)
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

            var node = _nodes.SingleOrDefault(n => n.Config.NodeId == args[0]);

            if (node == null) { return false; }

            node.SocketClient.Shutdown().GetAwaiter().GetResult();
            _nodes.Remove(node);
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
            var nodeId = ((GetInfoOptions)opts).NodeId;

            //Perform validations required before a command call
            if (!ValidatePreCommand(nodeId))
            {
                return false;
            }

            try
            {
                var connectedNode = GetConnectedNode(nodeId);

                //send the message to the server by writing it to the channel
                var request = new VersionRequest();
                _rpcClient.SendMessage(connectedNode, request.ToAny());

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
            var nodeId = ((GetInfoOptions)opts).NodeId;

            //Perform validations required before a command call
            if (!ValidatePreCommand(nodeId))
            {
                return false;
            }

            try
            {
                var connectedNode = GetConnectedNode(nodeId);

                //send the message to the server by writing it to the channel
                var request = new GetInfoRequest();
                _rpcClient.SendMessage(connectedNode, request.ToAny()).Wait();
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
            var nodeId = ((GetInfoOptions)args).NodeId;

            try
            {
                var connectedNode = GetConnectedNode(nodeId);

                //send the message to the server by writing it to the channel
                var request = new GetMempoolRequest();
                _rpcClient.SendMessage(connectedNode, request.ToAny()).Wait();
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
            Debug.Assert(nodeId != null, nameof(nodeId) + " != null");

            return (GetNodeConfig(nodeId) != null);
        }

        /// <summary>
        /// Checks if the node exists in the list of connected nodes.
        /// </summary>
        /// <param name="nodeId">The name of the node as entered at the command line</param>
        /// <returns>True if the node is existing in the connected nodes list and False otherwise</returns>
        public override bool IsConnectedNode(string nodeId)
        {
            Debug.Assert(nodeId != null, nameof(nodeId) + " != null");

            //if the node is in the list of connected nodes then a connection has already been established to it
            return (GetConnectedNode(nodeId) != null);
        }

        public override bool IsSocketChannelActive(IRpcNode node)
        {
            if (node.SocketClient.Channel.Active) { return true; }

            _logger.Information("Channel inactive ...");
            return false;
        }

        private bool ValidatePreCommand(string nodeId)
        {
            //if the node is invalid then do not continue
            if (!IsConfiguredNode(nodeId))
            {
                ReturnUserMessage(NO_CONFIG_MESSAGE);
                return false;
            }

            //Check if the node is already connected otherwise do not continue
            //if the node is already connected the method will return the instance
            if (!IsConnectedNode(nodeId))
            {
                ReturnUserMessage(NODE_NOT_CONNECTED_MESSAGE);
                return false;
            }

            var connectedNode = GetConnectedNode(nodeId);

            //Check if the channel is still active
            if (!IsSocketChannelActive(connectedNode))
            {
                ReturnUserMessage(CHANNEL_INACTIVE_MESSAGE);
                return false;
            }

            return true;
        }

        public override IRpcNode GetConnectedNode(string nodeId)
        {
            return _nodes.SingleOrDefault(node => node.Config.NodeId.Equals(nodeId));
        }

        public override IRpcNodeConfig GetNodeConfig(string nodeId)
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

        public void OnNext(IChanneledMessage<Any> value)
        {
            if (value == null) {return;}

            Response = value;
        }
    }
}
