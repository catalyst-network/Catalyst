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
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Cycle;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Core.Modules.Consensus.Cycle;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Core.Modules.Consensus.Deltas.Building;
using Catalyst.Core.Modules.Consensus.IO.Observers;
using Catalyst.Core.Modules.Kvm;

namespace Catalyst.Core.Modules.Consensus
{
    public class ConsensusModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // we will put this in its own module eventually
            builder.RegisterType<DateTimeProvider>().As<IDateTimeProvider>();
            builder.RegisterType<CycleSchedulerProvider>().As<ICycleSchedulerProvider>();
            builder.RegisterType<CycleEventsProviderRaw>().As<ICycleEventsProvider>();
            builder.RegisterType<DeltaCacheChangeTokenProvider>().As<IDeltaCacheChangeTokenProvider>().WithParameter("timeToLiveInMs", 600000);
            builder.RegisterType<FavouriteDeltaObserver>().As<IP2PMessageObserver>();
            builder.RegisterType<DeltaDfsHashObserver>().As<IP2PMessageObserver>();
            builder.RegisterType<CandidateDeltaObserver>().As<IP2PMessageObserver>();
            builder.RegisterType<DeltaProducersProvider>().As<IDeltaProducersProvider>();
            builder.RegisterType<DeltaElector>().As<IDeltaElector>().SingleInstance();
            builder.RegisterType<DeltaHashProvider>().As<IDeltaHashProvider>().SingleInstance().WithParameter("capacity", 10_000);
            builder.RegisterType<DeltaCache>().As<IDeltaCache>().SingleInstance()
               .WithExecutionParameters(builder)
               .WithStateDbParameters(builder);

            builder.RegisterType<DeltaVoter>().As<IDeltaVoter>().SingleInstance();
            builder.RegisterType<TransactionComparerByPriceAndHash>().As<ITransactionComparer>();
            builder.RegisterType<DeltaHub>().As<IDeltaHub>().SingleInstance();
            builder.RegisterType<DeltaTransactionRetriever>().As<IDeltaTransactionRetriever>().SingleInstance();
            builder.RegisterType<CycleSchedulerProvider>().As<ICycleSchedulerProvider>();
            builder.RegisterType<Consensus>().As<IConsensus>().SingleInstance();
            builder.RegisterInstance(CycleConfiguration.Default).As<ICycleConfiguration>();

            builder.RegisterType<DeltaBuilder>().As<IDeltaBuilder>().SingleInstance()
               .WithExecutionParameters(builder);
        }
    }
}
