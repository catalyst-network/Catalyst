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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Consensus.Cycle;
using Catalyst.Core.Consensus.Deltas;
using Catalyst.Core.Mempool.Documents;
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Core.Consensus
{
    public class ConsensusModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            builder.Register(c => new DeltaElector(
                    c.Resolve<IMemoryCache>(),
                    c.Resolve<IDeltaProducersProvider>(),
                    c.Resolve<ILogger>()
                ))
                .As<IDeltaElector>().SingleInstance();
            
            builder.Register(c => new DeltaHashProvider(
                    c.Resolve<IDeltaCache>(),
                    c.Resolve<ILogger>(),
                    10_000
                ))
                .As<IDeltaHashProvider>().SingleInstance();
            
            builder.Register(c => new DeltaCache(
                    c.Resolve<IMemoryCache>(),
                    c.Resolve<IDeltaDfsReader>(),
                    c.Resolve<IDeltaCacheChangeTokenProvider>(),
                    c.Resolve<ILogger>()
                ))
                .As<IDeltaCache>().SingleInstance();
            
            builder.Register(c => new DeltaVoter(
                    c.Resolve<IMemoryCache>(),
                    c.Resolve<IDeltaProducersProvider>(),
                    c.Resolve<IPeerIdentifier>(),
                    c.Resolve<ILogger>()
                ))
                .As<IDeltaVoter>().SingleInstance();
            
            builder.Register(c => new DeltaVoter(
                    c.Resolve<IMemoryCache>(),
                    c.Resolve<IDeltaProducersProvider>(),
                    c.Resolve<IPeerIdentifier>(),
                    c.Resolve<ILogger>()
                    ))
                .As<IDeltaVoter>().SingleInstance();
            
            builder.Register(c => new TransactionComparerByFeeTimestampAndHash())
                .As<ITransactionComparer>();
            
            builder.Register(c => new DeltaHub(
                    c.Resolve<IBroadcastManager>(),
                    c.Resolve<IPeerIdentifier>(),
                    c.Resolve<IDfs>(),
                    c.Resolve<ILogger>()
                ))
                .As<IDeltaHub>().SingleInstance();
            
            builder.Register(c => new DeltaTransactionRetriever(
                    c.Resolve<IMempool<MempoolDocument>>(),
                    c.Resolve<ITransactionComparer>()
                    ))
                .As<IDeltaTransactionRetriever>().SingleInstance();
            
            builder.Register(c => new DeltaBuilder(
                    c.Resolve<IDeltaTransactionRetriever>(),
                    c.Resolve<IDeterministicRandomFactory>(),
                    c.Resolve<IMultihashAlgorithm>(),
                    c.Resolve<IPeerIdentifier>(),
                    c.Resolve<IDeltaCache>(),
                    c.Resolve<IDateTimeProvider>(),
                    c.Resolve<ILogger>()
                ))
                .As<IDeltaBuilder>().SingleInstance();

            builder.Register(c => new CycleSchedulerProvider())
                .As<ICycleSchedulerProvider>();
            
            builder.Register(c => new CycleEventsProvider(
                c.Resolve<ICycleConfiguration>(),
                c.Resolve<IDateTimeProvider>(),
                c.Resolve<ICycleSchedulerProvider>(),
                c.Resolve<IDeltaHashProvider>(),
                c.Resolve<ILogger>()
            )).As<ICycleEventsProvider>();

            builder.Register(c => new Consensus(
                c.Resolve<IDeltaBuilder>(),
                c.Resolve<IDeltaVoter>(),
            c.Resolve<IDeltaElector>(),
            c.Resolve<IDeltaCache>(),
            c.Resolve<IDeltaHub>(),
            c.Resolve<ICycleEventsProvider>(),
            c.Resolve<IDeltaHashProvider>(),
            c.Resolve<ILogger>()
            )).As<IConsensus>().SingleInstance();
            
            base.Load(builder);
        }
    }
}
