/**
 * (C) Copyright 2019 Catalyst-Network
 *
 * Author USER ${USER}$
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; version 2
 * of the License.
 */

using System.Linq;
using System.Net;
using Catalyst.Node.Common.Helpers.Network;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Common.Helpers.Config
{
    public static class ConfigValueParser
    {
        public static IPEndPoint[] GetIpEndpointArrValues(IConfigurationRoot configurationRoot, string section)
        {
            return configurationRoot.GetSection("CatalystNodeConfiguration")
               .GetSection("Peer")
               .GetSection(section)
               .GetChildren()
               .Select(p => EndpointBuilder.BuildNewEndPoint(p.Value)).ToArray();
        }
        
        public static string[] GetStringArrValues(IConfigurationRoot configurationRoot, string section)
        {
            return configurationRoot.GetSection("CatalystNodeConfiguration")
               .GetSection("Peer")
               .GetSection(section)
               .GetChildren()
               .Select(p => p.Value).ToArray();
        }
    }
}
