using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;

namespace Catalyst.Common.Interfaces.Cli.Commands
{
    public interface ICommandContext
    {
        /// <summary>Gets the node RPC client factory.</summary>
        /// <value>The node RPC client factory.</value>
        INodeRpcClientFactory NodeRpcClientFactory { get; }

        /// <summary>Gets the certificate store.</summary>
        /// <value>The certificate store.</value>
        ICertificateStore CertificateStore { get; }

        /// <summary>Gets the user output.</summary>
        /// <value>The user output.</value>
        IUserOutput UserOutput { get; }

        /// <summary>Gets the socket client registry.</summary>
        /// <value>The socket client registry.</value>
        ISocketClientRegistry<INodeRpcClient> SocketClientRegistry { get; }

        /// <summary>Gets the peer client version.</summary>
        /// <value>The peer identifier peer client version.</value>
        IPeerIdClientId PeerIdClientId { get; }

        /// <summary>Gets the dto factory.</summary>
        /// <value>The dto factory.</value>
        IDtoFactory DtoFactory { get; }

        /// <summary>Gets the peer identifier.</summary>
        /// <value>The peer identifier.</value>
        IPeerIdentifier PeerIdentifier { get; }

        /// <summary>Gets the connected node.</summary>
        /// <param name="nodeId">The node identifier located in configuration.</param>
        /// <returns></returns>
        INodeRpcClient GetConnectedNode(string nodeId);

        /// <summary>Gets the node configuration.</summary>
        /// <param name="nodeId">The node identifier located in configuration.</param>
        /// <returns></returns>
        IRpcNodeConfig GetNodeConfig(string nodeId);

        /// <summary>Determines whether [is socket channel active] [the specified node].</summary>
        /// <param name="node">A <see cref="IRpcNode"/> object including node required information.</param>
        /// <returns><c>true</c> if [is socket channel active] [the specified node]; otherwise,
        /// <c>false</c> A "Channel inactive ..." message is returned to the console.</returns>
        bool IsSocketChannelActive(INodeRpcClient node);
    }
}
