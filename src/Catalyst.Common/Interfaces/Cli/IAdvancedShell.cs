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

using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.Rpc;

namespace Catalyst.Common.Interfaces.Cli
{
    public interface IAdvancedShell
        : IShell
    {
        /// <summary>
        /// Handles the command <code>get -i [node-name]</code>.  The method makes sure first the CLI is connected to
        /// the node specified in the command and then creates a <see cref="T:Catalyst.Protocol.Rpc.Node.GetInfoRequest" /> object and sends it in a
        /// message to the RPC server in the node.
        /// </summary>
        /// <param name="opts"><see cref="T:Catalyst.Cli.GetInfoOptions" /> object including the options entered through the CLI.</param>
        /// <returns>True if the message is sent successfully to the node and False otherwise.</returns>
        bool GetInfoCommand(IGetInfoOptions opts);

        /// <summary>
        /// Gets the version of a node
        /// </summary>
        /// <returns>Returns true if successful and false otherwise.</returns>
        bool GetVersionCommand(IGetVersionOptions opts);

        /// <summary>
        /// Handles the command <code>get -m [node-name]</code>.  The method makes sure first the CLI is connected to
        /// the node specified in the command and then creates a <see cref="T:Catalyst.Protocol.Rpc.Node.GetMempoolRequest" /> object and sends it in a
        /// message to the RPC server in the node.
        /// </summary>
        /// <param name="opts"><see cref="T:Catalyst.Cli.GetInfoOptions" /> object including the options entered through the CLI.</param>
        /// <returns>True if the message is sent successfully to the node and False otherwise.</returns>
        bool GetMempoolCommand(IGetMempoolOptions opts);

        /// <summary>
        /// Calls the specific option handler method from one of the "sign" command options based on the options passed
        /// in by he user through the command line.  The available options are:
        /// 1- sign message
        /// </summary>
        /// <param name="opts">An object of <see cref="ISignOptions"/> populated by the parser</param>
        /// <returns>Returns true if the command was correctly handled. This does not mean that the command ended successfully.
        /// Error messages returned to the user is considered a correct command handling</returns>
        bool MessageSignCommand(ISignOptions opts);

        /// <summary>
        /// Calls the specific option handler method from one of the "sign" command options based on the options passed
        /// in by he user through the command line.  The available options are:
        /// 1- sign message
        /// </summary>
        /// <param name="opts">An object of <see cref="IVerifyOptions"/> populated by the parser</param>
        /// <returns>Returns true if the command was correctly handled. This does not mean that the command ended successfully.
        /// Error messages returned to the user is considered a correct command handling</returns>
        bool MessageVerifyCommand(IVerifyOptions opts);

        /// <summary>
        /// Called when [peer list commands].
        /// </summary>
        /// <param name="opts">The options.</param>
        /// <returns>[true] if correct arguments, [false] if arguments are invalid</returns>
        bool PeerListCommand(IPeerListOptions opts);

        /// <summary>
        /// Called when [peer list commands].
        /// </summary>
        /// <param name="opts">The options.</param>
        /// <returns>[true] if correct arguments, [false] if arguments are invalid</returns>
        bool PeerCountCommand(IPeerCountOptions opts);

        /// <summary>Called when [remove peer commands].</summary>
        /// <param name="opts">The options.</param>
        /// <returns></returns>
        bool PeerRemoveCommand(IRemovePeerOptions opts);
        
        /// <param name="opts">The options.</param>
        /// <returns></returns>
        bool PeerReputationCommand(IPeerReputationOptions opts);

        /// <param name="opts">The options.</param>
        /// <returns></returns>
        bool PeerBlackListingCommand(IPeerBlackListingOptions opts);

        /// <param name="opts">The options.</param>
        /// <returns></returns>
        bool AddFile(IAddFileOnDfsOptions opts);

        INodeRpcClient GetConnectedNode(string nodeId);
    }
}
