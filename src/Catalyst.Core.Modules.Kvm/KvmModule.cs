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
using Autofac.Core;
using Catalyst.Abstractions.Kvm;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.Store;
using Module = Autofac.Module;

namespace Catalyst.Core.Modules.Kvm
{
    public class KvmModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CatalystSpecProvider>().As<ISpecProvider>();

            builder.RegisterType<StateUpdateHashProvider>().As<IStateUpdateHashProvider>().SingleInstance();
            builder.RegisterInstance(LimboLogs.Instance).As<ILogManager>().SingleInstance();

            builder.RegisterType<MemDb>().As<IDb>().SingleInstance();               // code db
            builder.RegisterType<StateDb>().As<ISnapshotableDb>().SingleInstance(); // state db
        }
    }

    public static class ExecutionRegistrations
    {
        /// <summary>
        /// Registers <see cref="IStateProvider"/>, <see cref="IStorageProvider"/>, <see cref="IKvm"/> and <see cref="IDeltaExecutor"/> components under provided <param serviceName="serviceName"></param>
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="serviceName">The serviceName under which the components are registered. </param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static ContainerBuilder RegisterExecutionComponents(this ContainerBuilder builder, string serviceName)
        {
            var stateProvider = new ByTypeNamedParameter<IStateProvider>(serviceName);
            var storageProvider = new ByTypeNamedParameter<IStorageProvider>(serviceName);
            var kvm = new ByTypeNamedParameter<IKvm>(serviceName);

            builder.RegisterType<StateProvider>().Named<IStateProvider>(serviceName).SingleInstance();
            builder.RegisterType<StorageProvider>().Named<IStorageProvider>(serviceName).SingleInstance()
               .WithParameter(stateProvider);
            builder.RegisterType<KatVirtualMachine>().Named<IKvm>(serviceName).SingleInstance()
               .WithParameter(stateProvider)
               .WithParameter(storageProvider);
            builder.RegisterType<DeltaExecutor>().Named<IDeltaExecutor>(serviceName).SingleInstance()
               .WithParameter(stateProvider)
               .WithParameter(storageProvider)
               .WithParameter(kvm);

            return builder;
        }

        /// <summary>
        /// Resolves a parameter of specific type by Name
        /// </summary>
        /// <typeparam serviceName="T"></typeparam>
        sealed class ByTypeNamedParameter<T> : ResolvedParameter
        {
            public ByTypeNamedParameter(string name) : base((p, _) => p.ParameterType == typeof(T), (_, c) => c.ResolveNamed<T>(name)) { }
        }
    }
}
