using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Catalyst.Node.Core.RPC.Authentication
{
    public class AuthCredentials
    {
        public string PublicKey { get; set; }
        public IPAddress IpAddress { get; set; }
    }
}
