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
using Catalyst.Abstractions.Kvm;
using Nethermind.Core.Crypto;
using Nethermind.Evm.Tracing;
using Nethermind.State;
using Serilog;
using Serilog.Events;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class DeltaStateCalculationStep : IDeltaBuilderStep
    {
        private readonly IStateProvider _stateProvider;
        private readonly IDeltaExecutor _deltaExecutor;
        private readonly ILogger _logger;

        public DeltaStateCalculationStep(IStateProvider stateProvider, IDeltaExecutor deltaExecutor, ILogger logger)
        {
            // note that this mus be a different state provider and a different executor
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _deltaExecutor = deltaExecutor ?? throw new ArgumentNullException(nameof(deltaExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Execute(DeltaBuilderContext context)
        {
            var previousRoot = context.PreviousDelta.StateRoot;
            Keccak stateRoot = previousRoot.IsEmpty ? Keccak.EmptyTreeHash : new Keccak(previousRoot.ToByteArray());
            
            if (_logger.IsEnabled(LogEventLevel.Debug)) _logger.Error($"Running state calculation for delta {context.ProducedDelta.DeltaNumber}");

            // here we need a read only delta executor (like in block builders - everything reverts in the end)
            _stateProvider.StateRoot = stateRoot;

            var callOutputTracer = new CallOutputTracer();
            _deltaExecutor.CallAndReset(context.ProducedDelta, callOutputTracer);
            _stateProvider.Reset();

            context.ProducedDelta.GasUsed = callOutputTracer.GasSpent;
        }
    }
}
