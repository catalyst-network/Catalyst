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
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Catalyst.Abstractions.Kvm;
using Catalyst.Core.Lib.FileSystem;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.Db.Rocks;
using Nethermind.Db.Rocks.Config;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.State;
using Module = Autofac.Module;

namespace Catalyst.Core.Modules.Kvm
{
    public class KvmModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CatalystSpecProvider>().As<ISpecProvider>();

            // builder.RegisterInstance(new OneLoggerLogManager(new SimpleConsoleLogger())).As<ILogManager>();
            builder.RegisterInstance(LimboLogs.Instance).As<ILogManager>();

            var catDir = new FileSystem().GetCatalystDataDir().FullName;
            // TODO
            //  builder.RegisterInstance(new StateDb(new CodeRocksDb(catDir, DbConfig.Default))).As<IDb>().SingleInstance();
            //  builder.RegisterInstance(new StateDb(new StateRocksDb(catDir, DbConfig.Default))).As<ISnapshotableDb>().SingleInstance();
            //builder.RegisterInstance(new MemDb()).As<IDb>().SingleInstance();               // code db
            //builder.RegisterInstance(new StateDb()).As<ISnapshotableDb>().SingleInstance(); // state db

            builder.RegisterType<StateReader>().As<IStateReader>(); // state db
        }
    }

    public static class ExecutionRegistrations
    {
        /// <summary>
        /// Registers a custom set of the following components: <see cref="IStateProvider"/>, <see cref="IStorageProvider"/>, <see cref="IKvm"/> and <see cref="IDeltaExecutor"/> for the given <paramref name="registration"/>.
        /// </summary>
        /// <param name="registration">The registration to be enhanced.</param>
        /// <param name="builder">The container builder.</param>
        /// <returns>The <paramref name="registration"/>.</returns>
        public static IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> WithExecutionParameters<TLimit, TReflectionActivatorData, TStyle>(this IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> registration, ContainerBuilder builder)
            where TReflectionActivatorData : ReflectionActivatorData
        {
            var serviceName = Guid.NewGuid().ToString();

            var stateProvider = new ByTypeNamedParameter<IWorldState>(serviceName);
            var kvm = new ByTypeNamedParameter<IKvm>(serviceName); 
            var executor = new ByTypeNamedParameter<IDeltaExecutor>(serviceName); 

            builder.RegisterType<WorldState>().Named<IWorldState>(serviceName).SingleInstance()
               .WithParameter(stateProvider);
            builder.RegisterType<KatVirtualMachine>().Named<IKvm>(serviceName).SingleInstance()
               .WithParameter(stateProvider);
            builder.RegisterType<DeltaExecutor>().Named<IDeltaExecutor>(serviceName).SingleInstance()
               .WithParameter(stateProvider)
               .WithParameter(kvm);

            // parameter registration
            registration
               .WithParameter(stateProvider)
               .WithParameter(kvm)
               .WithParameter(executor);

            return registration;
        }

        /// <summary>
        /// Resolves a parameter of specific type with its named service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        sealed class ByTypeNamedParameter<T> : ResolvedParameter
        {
            public ByTypeNamedParameter(string name) : base((p, _) => p.ParameterType == typeof(T), (_, c) => c.ResolveNamed<T>(name)) { }
        }
    }
}
