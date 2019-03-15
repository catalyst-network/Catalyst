using Catalyst.Node.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using Dawn;

namespace Catalyst.Node.Core.RPC
{
    public class CLIRPCServerSettings : ICLIRPCServerSettings
    {
        public CLIRPCServerSettings(IConfigurationRoot rootSection)
        {
            //Get the configuration
            Guard.Argument(rootSection, nameof(rootSection)).NotNull();

            //To Be Changed to read CLI RPC Server Settings
            var section = rootSection.GetSection("CatalystNodeConfiguration").GetSection("RPC");

            //Set the port number from server settings
            Port = int.Parse(section.GetSection("Port").Value);

            CertFileName = section.GetSection("CertFileName").Value;

            //Set the SSL Certificate password.  I don't know how to use this yet!!!
            SslCertPassword = section.GetSection("SslCertPassword").Value;

            //Set the server address
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
        }

        public int Port { get; set;}
        public IPAddress BindAddress { get; set;}

        public string SslCertPassword { get; set;}

        public string CertFileName { get; set; }

        public static bool UseLibuv
        {
            get
            {
                string libuv = Helper.Configuration["libuv"];
                return !string.IsNullOrEmpty(libuv) && bool.Parse(libuv);
            }
        }
    }
}