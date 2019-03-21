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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.Shell;

using Microsoft.Extensions.Configuration;


using Dawn;

namespace Catalyst.Cli
{

    /// <summary>
    /// This class provides the settings for the CLI.
    /// </summary>
    public class RpcNodes : IRpcNodes
    {
        public List<RpcNode> nodesList { get; }
        
        private readonly IConfigurationRoot _configurationRoot;
        
        /// <summary>
        /// Intializes a new instance of the ClientSettings class and passes the application configuration
        /// </summary>
        /// <param name="rootSection"></param>
        public RpcNodes(IConfigurationRoot rootSection)
        {
            _configurationRoot = rootSection;
            nodesList = new List<RpcNode>();
            
            BuildRpcNodes();

        }
        
        private void BuildRpcNodes()
        {
            var section = _configurationRoot.GetSection("CatalystCliRpcNodes").GetSection("nodes");

            foreach (var child in section.GetChildren())
            {
                RpcNode node = new RpcNode();
                
                node.NodeId = child.GetSection("nodeId").Value;
                
                node.HostAddress = IPAddress.Parse(child.GetSection("host").Value);
                
                node.Port = int.Parse(child.GetSection("port").Value);
                
                node.PfxFileName = child.GetSection("PfxFileName").Value;
                
                node.SslCertPassword = child.GetSection("SslCertPassword").Value;
                
                nodesList.Add(node);
            }
        }
    }
}