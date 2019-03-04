using System.Collections.Generic;
using System.Net;

namespace Catalyst.Node.Common.P2P
{
    public interface IPeerSettings
    {
        Network Network { get; }
        string PayoutAddress { get; }
        string PublicKey { get; }
        bool Announce { get; }
        IPEndPoint AnnounceServer { get; }
        bool MutualAuthentication { get; }
        bool AcceptInvalidCerts { get; }
        ushort MaxConnections { get; }
        int Port { get; }
        IPAddress BindAddress { get; }
        IPEndPoint EndPoint { get; }
        int Magic { get; }
        string PfxFileName { get; }
        List<string> KnownNodes { get; }
        List<string> SeedServers { get; }
        byte AddressVersion { get; }
        string SslCertPassword { get; }
    }
}