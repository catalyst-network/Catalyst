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

using Autofac;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.BlockExchange;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Kernel;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using Catalyst.Core.Modules.Dfs.CoreApi;
using Catalyst.Core.Modules.Keystore;
using Lib.P2P;
using Makaretu.Dns;
using Nito.AsyncEx;

namespace Catalyst.Core.Modules.Dfs
{
    public sealed class DfsModule : CatalystModule
    {
        protected override void LoadApi(ContainerBuilder builder)
        {
            builder.RegisterType<BlockApi>().As<IBlockApi>().SingleInstance().OnActivated(e => e.Instance.PinApi = e.Context.Resolve<IPinApi>()).PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<PinApi>().As<IPinApi>().SingleInstance().OnActivated(e => e.Instance.BlockApi = e.Context.Resolve<IBlockApi>()).PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
            builder.RegisterType<BitSwapApi>().As<IBitswapApi>().SingleInstance();
            builder.RegisterType<BlockRepositoryApi>().As<IBlockRepositoryApi>().SingleInstance();
            builder.RegisterType<BootstrapApi>().As<IBootstrapApi>().SingleInstance();
            builder.RegisterType<ConfigApi>().As<IConfigApi>().SingleInstance();
            builder.RegisterType<DagApi>().As<IDagApi>().SingleInstance();
            builder.RegisterType<DnsApi>().As<IDnsApi>().SingleInstance();
            builder.RegisterType<UnixFsApi>().As<IUnixFsApi>().SingleInstance();
            builder.RegisterType<KeyApi>().As<IKeyApi>().SingleInstance();
            builder.RegisterType<NameApi>().As<INameApi>().SingleInstance();
            builder.RegisterType<ObjectApi>().As<IObjectApi>().SingleInstance();
            builder.RegisterType<PubSubApi>().As<IPubSubApi>().SingleInstance();
            builder.RegisterType<StatsApi>().As<IStatsApi>().SingleInstance();
            builder.RegisterType<SwarmApi>().As<ISwarmApi>().SingleInstance();
        }

        protected override void LoadService(ContainerBuilder builder) 
        { 
            builder.RegisterType<DfsService>().As<IDfsService>().SingleInstance();
            builder.RegisterType<BitswapService>().As<IBitswapService>().SingleInstance();
            builder.RegisterType<SwarmService>().As<ISwarmService>().SingleInstance();
            
            builder.RegisterType<DotClient>().As<IDnsClient>();
        }
        
        protected override void LoadOptions(ContainerBuilder builder) 
        { 
            builder.RegisterType<BlockOptions>();
            builder.RegisterType<RepositoryOptions>();
            builder.RegisterType<DiscoveryOptions>();
        }
    }
}
