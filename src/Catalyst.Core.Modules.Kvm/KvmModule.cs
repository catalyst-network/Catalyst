#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.Db.Rocks;
using Nethermind.Db.Rocks.Config;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.State;
using Nethermind.Trie;
using Nethermind.Trie.Pruning;
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

// TNA TODO
//            DbSettings stateDbSettings = BuildDbSettings(DbNames.State, () => Nethermind.Db.Metrics.StateDbReads++, () => Nethermind.Db.Metrics.StateDbWrites++);

            var catDir = new FileSystem().GetCatalystDataDir().FullName;
//            builder.RegisterInstance(new CodeRocksDb(catDir, stateDbSettings, DbConfig.Default, LimboLogs.Instance)).As<IDb>().SingleInstance();
            builder.RegisterType<StateReader>().As<IStateReader>();
        }

        private static DbSettings BuildDbSettings(string dbName, Action updateReadsMetrics, Action updateWriteMetrics, bool deleteOnStart = false)
        {
            return new(GetTitleDbName(dbName), dbName)
           {
                // TNA TODO
                //               UpdateReadMetrics = updateReadsMetrics,
                //               UpdateWriteMetrics = updateWriteMetrics,
                DeleteOnStart = deleteOnStart
            };
        }

        protected static string GetTitleDbName(string dbName) => char.ToUpper(dbName[0]) + dbName[1..];
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

            var worldStateProvider = new ByTypeNamedParameter<IWorldState>(serviceName);
            var kvm = new ByTypeNamedParameter<IKvm>(serviceName);
            var executor = new ByTypeNamedParameter<IDeltaExecutor>(serviceName);

            
            var KeyValueStoreWithBatching = new ByTypeNamedParameter<IKeyValueStoreWithBatching>(serviceName);
            
            var trieStore = new ByTypeNamedParameter<ITrieStore>(serviceName);
            var keyValueStore = new ByTypeNamedParameter<IKeyValueStore>(serviceName);
            var logger = new ByTypeNamedParameter<ILogManager>(serviceName);

            builder.RegisterType<Db>().Named<IKeyValueStoreWithBatching>(serviceName).SingleInstance();

            builder.RegisterType<TrieStore>().Named<ITrieStore>(serviceName).SingleInstance()
                .WithParameter(KeyValueStoreWithBatching);

            builder.RegisterType<KeyValueStore>().Named<IKeyValueStore>(serviceName).SingleInstance();
            builder.RegisterType<Logs>().Named<ILogManager>(serviceName).SingleInstance();

            builder.RegisterType<WorldState>().Named<IWorldState>(serviceName).SingleInstance()
               .WithParameter(trieStore)
               .WithParameter(keyValueStore)
               .WithParameter(logger);
            builder.RegisterType<KatVirtualMachine>().Named<IKvm>(serviceName).SingleInstance()
               .WithParameter(worldStateProvider);
            builder.RegisterType<DeltaExecutor>().Named<IDeltaExecutor>(serviceName).SingleInstance()
               .WithParameter(worldStateProvider)
               .WithParameter(kvm);

            // parameter registration
            registration
               .WithParameter(worldStateProvider)
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
