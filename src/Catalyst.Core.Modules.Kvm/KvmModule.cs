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
using Catalyst.Abstractions.Kvm;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.Store;

namespace Catalyst.Core.Modules.Kvm
{
    public class KvmModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DeltaExecutor>().As<IDeltaExecutor>().SingleInstance();
            builder.RegisterType<KatVirtualMachine>().As<IKvm>().SingleInstance();
            builder.RegisterType<CatalystSpecProvider>().As<ISpecProvider>();

            builder.RegisterType<StateProvider>().As<IStateProvider>().SingleInstance();
            builder.RegisterType<StorageProvider>().As<IStorageProvider>().SingleInstance();
            builder.RegisterType<StateUpdateHashProvider>().As<IStateUpdateHashProvider>().SingleInstance();
            builder.RegisterInstance(LimboLogs.Instance).As<ILogManager>();
            
            builder.RegisterInstance(new MemDb()).As<IDb>(); // code db
            builder.RegisterInstance(new StateDb()).As<ISnapshotableDb>(); // state db
            
            builder.RegisterType<StateReader>().As<IStateReader>(); // state db
            
            // builder.RegisterType<EthRpcService>().As<IEthRpcService>().SingleInstance();
        }  
    }
}
