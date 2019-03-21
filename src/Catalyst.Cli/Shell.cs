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
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Shell;
using Catalyst.Node.Common.Interfaces;
using Dawn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;

namespace Catalyst.Cli
{
    public sealed class Shell : ShellBase, IAds
    {
        private readonly IRpcNodes _rpcNodes;
        private readonly IRpcClient _rpcClient;

        /// <summary>
        /// </summary>
        public Shell(IRpcClient rpcClient, IRpcNodes rpcNodes)
        {
            _rpcNodes = rpcNodes;
            _rpcClient = rpcClient;
            
            Console.WriteLine(@"Koopa Shell Start");
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
        public bool OnPeerCommand(string[] args)
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
        protected override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower(AppCulture))
            {
                case "connect":
                    return OnConnectNode(args);
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
        private bool OnConnectNode(string[] args)
        {
            //check if the args array is not null, not empty and of minimum count 2
            Guard.Argument(args, nameof(args)).NotNull().NotEmpty().MinCount(3);
            
            //Get the node entered by the user from the nodes list
            RpcNode connectingNode = _rpcNodes.nodesList.Find(node => node.NodeId.Equals((args[2])));

            try
            {
                Task.WaitAll(_rpcClient.RunClientAsync(connectingNode));
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
                    return base.OnCommand(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStopNode(string[] args)
        {
            Guard.Argument(args).Contains(typeof(string));
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStopWork(string[] args)
        {
            Guard.Argument(args).Contains(typeof(string));
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnGetCommand(string[] args)
        {
            Guard.Argument(args).Contains(typeof(string));
            switch (args[1].ToLower(AppCulture))
            {
                case "delta":
                    return OnGetDelta(args);
                case "mempool":
                    return OnGetMempool();
                case "version":
                    return OnGetVersion(args);
                default:
                    return base.OnCommand(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected override bool OnGetInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the version of a node
        /// </summary>
        /// <returns>Returns true if successful and false otherwise.</returns>
        protected override bool OnGetVersion(string[] args) { return GetNodeInfo(args); }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected override bool OnGetConfig(string[] args) { return GetNodeInfo(args); }

        /// <summary>
        /// Requests node related information from the RPC Server.  The informaiton returned depends on the command in
        /// the argument list which can be:
        /// 1- version: gets node version
        /// 2- config: gets node config
        /// 3- info: gets node info
        /// </summary>
        /// <param name="args">Array of strings including the commands entered through command line interface</param>
        /// <returns>Returns true if successful and false otherwise.</returns>
        /// <exception cref="Exception"></exception>
        private bool GetNodeInfo(string[] args)
        {
            try
            {
                //check if the args array is not null, not empty and of minimum count 2
                Guard.Argument(args, nameof(args)).NotNull().NotEmpty().MinCount(3);

                //get the node 
                RpcNode nodeConnected = _rpcClient.GetConnectedNode(args[2]);

                //check if the node entered by the user was in the list of the connected nodes
                if (nodeConnected != null)
                {
                    //send the message to the server by writing it to the channel
                    _rpcClient.SendMessage(nodeConnected, args[1]);
                }
                else
                {
                    Console.WriteLine("Node not found.  Please connect to node first.");
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                throw e;
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
        ///     Get stats about the underlying mempool implementation
        /// </summary>
        /// <returns>Boolean</returns>
        protected override bool OnGetMempool()
        {
            throw new NotImplementedException();
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
    }
}