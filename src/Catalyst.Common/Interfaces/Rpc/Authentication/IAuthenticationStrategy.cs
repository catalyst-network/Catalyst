using Catalyst.Common.Interfaces.P2P;

namespace Catalyst.Common.Interfaces.Rpc.Authentication
{
    public interface IAuthenticationStrategy
    {
        /// <summary>Authenticates the specified peer identifier.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <returns></returns>
        bool Authenticate(IPeerIdentifier peerIdentifier);
    }
}
