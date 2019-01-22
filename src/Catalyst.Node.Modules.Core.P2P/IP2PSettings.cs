using System;
using System.Collections.Generic;

namespace Catalyst.Node.Modules.Core.P2P
{
    public interface IP2PSettings
    {
        string Network { get; set; }
        bool MutualAuthentication { get; set; }
        bool AcceptInvalidCerts { get; set; }
        int Port { get; set; }
        uint Magic { get; set; }
        string BindAddress { get; }
        List<string> SeedList { get; set; }
        string PfxFileName { get; set; }
        byte AddressVersion { get; set; }
        string SslCertPassword { get; set; }
    }
}