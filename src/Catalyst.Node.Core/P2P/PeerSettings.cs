using System.Collections.Generic;
using System.Linq;
using System.Net;
using Catalyst.Node.Core.Helpers.Network;
using Dawn;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core.P2P
{
    /// <summary>
    /// Peer settings class.
    /// </summary>
    public class PeerSettings : IPeerSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal PeerSettings(IConfiguration section)
        {
            Guard.Argument(section, nameof(section)).NotNull();
            Network = section.GetSection("Network").Value;
            PublicKey = section.GetSection("PublicKey").Value;
            Port = int.Parse(section.GetSection("Port").Value);
            Magic = int.Parse(section.GetSection("Magic").Value);
            PfxFileName = section.GetSection("PfxFileName").Value;
            PayoutAddress = section.GetSection("PayoutAddress").Value;
            Announce = bool.Parse(section.GetSection("Announce").Value);
            SslCertPassword = section.GetSection("SslCertPassword").Value;
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
            AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
            MaxConnections = ushort.Parse(section.GetSection("MaxConnections").Value);
            AcceptInvalidCerts = bool.Parse(section.GetSection("AcceptInvalidCerts").Value);
            MutualAuthentication = bool.Parse(section.GetSection("MutualAuthentication").Value);
            DnsServer = EndpointBuilder.BuildNewEndPoint(section.GetSection("DnsServer").Value);
            KnownNodes = section.GetSection("KnownNodes").GetChildren().Select(p => p.Value).ToList();
            SeedServers = section.GetSection("SeedServers").GetChildren().Select(p => p.Value).ToList();
            AnnounceServer =
                Announce ? EndpointBuilder.BuildNewEndPoint(section.GetSection("AnnounceServer").Value) : null;
        }

        public string Network { get; set; }
        public string PayoutAddress { get; set; }
        public string PublicKey { get; set; }
        public bool Announce { get; set; }
        public IPEndPoint DnsServer { get; set; }
        public IPEndPoint AnnounceServer { get; set; }
        public bool MutualAuthentication { get; set; }
        public bool AcceptInvalidCerts { get; set; }
        public ushort MaxConnections { get; set; }
        public int Port { get; set; }
        public int Magic { get; set; }
        public IPAddress BindAddress { get; set; }
        public string PfxFileName { get; set; }
        public List<string> KnownNodes { get; set; }
        public List<string> SeedServers { get; set; }
        public byte AddressVersion { get; set; }
        public string SslCertPassword { get; set; }
    }
}