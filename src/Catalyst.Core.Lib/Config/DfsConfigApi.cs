#region LICENSE

/**
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

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Options;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Lib.Config
{
    public sealed class DfsConfigApi : ConfigApiBase, IDfsConfigApi
    {
        public DfsConfigApi(DfsOptions dfsOptions) : base(Path.Combine(dfsOptions.Repository.Folder, "config")) {}
        
        private static readonly JObject DefaultConfiguration = JObject.Parse(@"{
          ""Addresses"": {
            ""API"": ""/ip4/127.0.0.1/tcp/5001"",
            ""Gateway"": ""/ip4/127.0.0.1/tcp/8080"",
            ""Swarm"": [
              ""/ip4/0.0.0.0/tcp/4001"",
              ""/ip6/::/tcp/4001""
            ]
          },
        }");
        
        protected override Task OnFileNotExisting()
        {
            return ReplaceAsync(DefaultConfiguration);
        }
        
    }
}
