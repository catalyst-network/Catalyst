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
using Catalyst.Abstractions.Contract;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.FileSystem;
using Catalyst.Core.Modules.Kvm.Validators;
using Catalyst.Module.ConvanSmartContract.Contract;
using Nethermind.Abi;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.Db.Rocks;
using Nethermind.Db.Rocks.Config;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.State;

namespace Catalyst.Core.Modules.Kvm
{
    public class KvmModule : Autofac.Module
    {
        private readonly bool _useInMemoryDb;
        public KvmModule() : this(false) { }

        public KvmModule(bool useInMemoryDb)
        {
            _useInMemoryDb = useInMemoryDb;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CatalystSpecProvider>().As<ISpecProvider>();

            builder.RegisterType<StateUpdateHashProvider>().As<IStateUpdateHashProvider>().SingleInstance();

            // builder.RegisterInstance(new OneLoggerLogManager(new SimpleConsoleLogger())).As<ILogManager>();
            builder.RegisterInstance(LimboLogs.Instance).As<ILogManager>();

            var catDir = new FileSystem().GetCatalystDataDir().FullName;
            var codeDb = _useInMemoryDb ? new MemDb() : (IDb)new CodeRocksDb(catDir, DbConfig.Default);
            var code = new StateDb(codeDb);
            var stateDb = _useInMemoryDb ? new MemDb() : (IDb)new StateRocksDb(catDir, DbConfig.Default);
            var state = new StateDb(stateDb);

            builder.RegisterInstance(code).As<IDb>().Named<IDb>("codeDb").SingleInstance();
            builder.RegisterInstance(state).As<IDb>().Named<IDb>("stateDb").SingleInstance();
            builder.RegisterInstance(code).As<ISnapshotableDb>().Named<ISnapshotableDb>("codeDb").SingleInstance();
            builder.RegisterInstance(state).As<ISnapshotableDb>().Named<ISnapshotableDb>("stateDb").SingleInstance();

            //builder.RegisterInstance(new MemDb()).As<IDb>().SingleInstance();               // code db
            //builder.RegisterInstance(new StateDb()).As<ISnapshotableDb>().SingleInstance(); // state db

            builder.RegisterType<StateReader>().As<IStateReader>().WithStateDbParameters(builder);

            builder.RegisterType<AbiEncoder>().As<IAbiEncoder>().SingleInstance();

            builder.RegisterType<ContractValidatorReader>().As<IValidatorReader>().SingleInstance().WithExecutionParameters(builder);

            builder.RegisterType<ValidatorSetContract>().As<IValidatorSetContract>().SingleInstance().WithExecutionParameters(builder);
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

            var stateProvider = new ByTypeNamedParameter<IStateProvider>(serviceName);
            var storageProvider = new ByTypeNamedParameter<IStorageProvider>(serviceName);
            var kvm = new ByTypeNamedParameter<IKvm>(serviceName);
            var executor = new ByTypeNamedParameter<IDeltaExecutor>(serviceName);

            builder.RegisterType<StateProvider>().Named<IStateProvider>(serviceName).SingleInstance()
            .WithStateDbParameters(builder);

            builder.RegisterType<StorageProvider>().Named<IStorageProvider>(serviceName).SingleInstance()
            .WithParameter(stateProvider)
            .WithStateDbParameters(builder);

            builder.RegisterType<KatVirtualMachine>().Named<IKvm>(serviceName).SingleInstance()
               .WithParameter(stateProvider)
               .WithParameter(storageProvider);
            builder.RegisterType<DeltaExecutor>().Named<IDeltaExecutor>(serviceName).SingleInstance()
               .WithParameter(stateProvider)
               .WithParameter(storageProvider)
               .WithParameter(kvm);

            // parameter registration
            registration
               .WithParameter(stateProvider)
               .WithParameter(storageProvider)
               .WithParameter(kvm)
               .WithParameter(executor);

            return registration;
        }

        public static IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> WithStateDbParameters<TLimit, TReflectionActivatorData, TStyle>(this IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> registration, ContainerBuilder builder)
    where TReflectionActivatorData : ReflectionActivatorData
        {
            registration
            .WithParameter(new ResolvedParameter((p, ctx) => p.Name == "codeDb", (p, ctx) => ctx.ResolveNamed<ISnapshotableDb>("codeDb")))
            .WithParameter(new ResolvedParameter((p, ctx) => p.Name == "codeDb", (p, ctx) => ctx.ResolveNamed<IDb>("codeDb")))
            .WithParameter(new ResolvedParameter((p, ctx) => p.Name == "stateDb", (p, ctx) => ctx.ResolveNamed<ISnapshotableDb>("stateDb")))
            .WithParameter(new ResolvedParameter((p, ctx) => p.Name == "stateDb", (p, ctx) => ctx.ResolveNamed<IDb>("stateDb")));

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
