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

using Catalyst.Cli.CommandTypes;
using Catalyst.Cli.Options;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Core.Network;
using Dawn;
using Serilog;

namespace Catalyst.Cli.Commands
{
    public class ConnectCommand : BaseCommand<ConnectOptions>
    {
        private readonly ILogger _logger;

        public ConnectCommand(ILogger logger, ICommandContext commandContext) : base(commandContext)
        {
            _logger = logger;
        }

        public static string InvalidSocketChannel => "Inactive socket channel.";

        protected override bool ExecuteCommand(ConnectOptions option)
        {
            var rpcNodeConfigs = CommandContext.GetNodeConfig(option.Node);
            Guard.Argument(rpcNodeConfigs, nameof(rpcNodeConfigs)).NotNull();

            //Connect to the node and store it in the socket client registry
            var nodeRpcClient = CommandContext.NodeRpcClientFactory.GetClient(
                CommandContext.CertificateStore.ReadOrCreateCertificateFile(rpcNodeConfigs.PfxFileName),
                rpcNodeConfigs).ConfigureAwait(false).GetAwaiter().GetResult();

            if (!CommandContext.IsSocketChannelActive(nodeRpcClient))
            {
                CommandContext.UserOutput.WriteLine(InvalidSocketChannel);
                return false;
            }

            var clientHashCode = CommandContext.SocketClientRegistry.GenerateClientHashCode(
                EndpointBuilder.BuildNewEndPoint(rpcNodeConfigs.HostAddress, rpcNodeConfigs.Port));

            CommandContext.SocketClientRegistry.AddClientToRegistry(clientHashCode, nodeRpcClient);
            CommandContext.UserOutput.WriteLine($"Connected to Node {nodeRpcClient.Channel.RemoteAddress}");
            return true;
        }
    }
}
