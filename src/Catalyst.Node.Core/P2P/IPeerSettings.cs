using System.Collections.Generic;
using System.Net;

namespace Catalyst.Node.Core.P2P {
    public interface IPeerSettings {
        string Network { get; set; }
        string PayoutAddress { get; set; }
        string PublicKey { get; set; }
        bool Announce { get; set; }
        IPEndPoint DnsServer { get; set; }
        IPEndPoint AnnounceServer { get; set; }
        bool MutualAuthentication { get; set; }
        bool AcceptInvalidCerts { get; set; }
        ushort MaxConnections { get; set; }
        int Port { get; set; }
        int Magic { get; set; }
        IPAddress BindAddress { get; set; }
        string PfxFileName { get; set; }
        List<string> KnownNodes { get; set; }
        List<string> SeedServers { get; set; }
        byte AddressVersion { get; set; }
        string SslCertPassword { get; set; }
    }
}