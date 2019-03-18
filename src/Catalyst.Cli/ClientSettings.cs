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

namespace Catalyst.Cli
{

    /// <summary>
    /// This class provides the settings for the CLI.
    /// </summary>
    public class ClientSettings : ICLIRPClientSettings
    {
        /// <summary>
        /// Intializes a new instance of the ClientSettings class and passes the application configuration
        /// </summary>
        /// <param name="rootSection"></param>
        public ClientSettings()
        {
            IConfigurationRoot rootSection = Helper.Configuration;
            
            //Get the configuration
            Guard.Argument(rootSection, nameof(rootSection)).NotNull();

            //Get the RPC Server Settings from the configuration file
            var section = rootSection.GetSection("CatalystNodeConfiguration").GetSection("RPCClient");

            //Set the port number from server settings
            Port = int.Parse(section.GetSection("Port").Value);

            CertFileName = section.GetSection("CertFileName").Value;

            //Set the SSL Certificate password.  I don't know how to use this yet!!!
            SslCertPassword = section.GetSection("SslCertPassword").Value;

            //Set the server address
            /*TBC: Should be an array to accomodate for connecting to more than one server/node. */
            ServerAddress = IPAddress.Parse(section.GetSection("ServerAddress").Value);
            
            Size = int.Parse(section.GetSection("Size").Value);

            isSSL = bool.Parse(section.GetSection("SSL").Value);
        }
        
        public bool IsSsl { get; set;}

        public IPAddress ServerAddress { get; set;}
        
        public int Port { get; set;}

        public int Size { get; set;}
        
        public string SslCertPassword { get; set;}

        public string CertFileName { get; set; }
        
        public bool isSSL { get; set; }

        public bool UseLibuv
        {
            get
            {
                string libuv = Helper.Configuration["libuv"];
                return !string.IsNullOrEmpty(libuv) && bool.Parse(libuv);
            }
        }
    }
}