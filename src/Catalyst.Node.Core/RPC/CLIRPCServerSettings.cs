/*
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

using Catalyst.Node.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using Dawn;

namespace Catalyst.Node.Core.RPC
{
    /// <summary>
    /// This class provides the settings for the CLIRPCServer class.
    /// </summary>
    public class CLIRPCServerSettings : ICLIRPCServerSettings
    {
        /// <summary>
        /// Intializes a new instance of the CLIRPCServerSettings by reading the settings from the devnet.json
        /// application configuration and sets the settings properties.
        /// </summary>
        /// <param name="rootSection"></param>
        public CLIRPCServerSettings(IConfigurationRoot rootSection)
        {
            //Get the configuration
            Guard.Argument(rootSection, nameof(rootSection)).NotNull();

            //Get the RPC Server Settings from the configuration file
            var section = rootSection.GetSection("CatalystNodeConfiguration").GetSection("RPC");

            //Set the port number from server settings
            Port = int.Parse(section.GetSection("Port").Value);

            CertFileName = section.GetSection("CertFileName").Value;

            //Set the SSL Certificate password.  I don't know how to use this yet!!!
            SslCertPassword = section.GetSection("SslCertPassword").Value;

            //Set the server address
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);

            isSSL = bool.Parse(section.GetSection("SSL").Value);
        }

        public int Port { get; set;}
        public IPAddress BindAddress { get; set;}

        public string SslCertPassword { get; set;}

        public string CertFileName { get; set; }
        
        public bool isSSL { get; set; }

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