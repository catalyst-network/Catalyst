using System;
using Catalyst.Cli.Options;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Network;
using Dawn;
using Serilog;

namespace Catalyst.Cli.Commands
{
    public class ConnectCommand : BaseOptionCommand<ConnectOptions>
    {
        private readonly ILogger _logger;

        protected override void ExecuteCommand(ConnectOptions option)
        {
            var rpcNodeConfigs = CommandContext.GetNodeConfig(option.Node);
            Guard.Argument(rpcNodeConfigs, nameof(rpcNodeConfigs)).NotNull();

            try
            {
                //Connect to the node and store it in the socket client registry
                var nodeRpcClient = CommandContext.NodeRpcClientFactory.GetClient(
                    CommandContext.CertificateStore.ReadOrCreateCertificateFile(rpcNodeConfigs.PfxFileName), rpcNodeConfigs);

                if (!CommandContext.IsSocketChannelActive(nodeRpcClient))
                {
                    CommandContext.UserOutput.WriteLine("Inactive socket channel.");
                    return;
                }

                var clientHashCode = CommandContext.SocketClientRegistry.GenerateClientHashCode(
                    EndpointBuilder.BuildNewEndPoint(rpcNodeConfigs.HostAddress, rpcNodeConfigs.Port));

                CommandContext.SocketClientRegistry.AddClientToRegistry(clientHashCode, nodeRpcClient);
                CommandContext.UserOutput.WriteLine($"Connected to Node {nodeRpcClient.Channel.RemoteAddress}");
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message, e);
            }
        }

        public ConnectCommand(ILogger logger, ICommandContext commandContext) : base(commandContext)
        {
            _logger = logger;
        }
    }
}
