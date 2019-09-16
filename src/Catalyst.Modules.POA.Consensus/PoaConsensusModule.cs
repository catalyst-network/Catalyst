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

using Autofac;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Modules.POA.Consensus.Deltas;
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Modules.POA.Consensus
{
    public class PoaConsensusModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PoaDeltaProducersProvider>().As<IDeltaProducersProvider>();
        }
    }
}