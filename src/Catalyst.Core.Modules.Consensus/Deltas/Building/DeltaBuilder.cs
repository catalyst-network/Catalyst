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

using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.P2P;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Lib.P2P;
using Nethermind.State;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    /// <inheritdoc />
    public sealed class DeltaBuilder : IDeltaBuilder
    {
        private readonly IDeltaTransactionRetriever _transactionRetriever;
        private readonly IDeterministicRandomFactory _randomFactory;
        private readonly IHashProvider _hashProvider;
        private readonly PeerId _producerUniqueId;
        private readonly IDeltaCache _deltaCache;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IWorldState _stateProvider;
        private readonly IDeltaExecutor _deltaExecutor;
        private readonly ILogger _logger;

        public DeltaBuilder(IDeltaTransactionRetriever transactionRetriever,
            IDeterministicRandomFactory randomFactory,
            IHashProvider hashProvider,
            IPeerSettings peerSettings,
            IDeltaCache deltaCache,
            IDateTimeProvider dateTimeProvider,
            IWorldState stateProvider,
            IDeltaExecutor deltaExecutor,
            ILogger logger)
        {
            _transactionRetriever = transactionRetriever;
            _randomFactory = randomFactory;
            _hashProvider = hashProvider;
            _producerUniqueId = peerSettings.PeerId;
            _deltaCache = deltaCache;
            _dateTimeProvider = dateTimeProvider;
            _stateProvider = stateProvider;
            _deltaExecutor = deltaExecutor;
            _logger = logger;

            PrepareSteps();
        }

        private void PrepareSteps()
        {
            _steps = new IDeltaBuilderStep[]
            {
                new PreviousDeltaResolverStep(_deltaCache, _logger),
                new TransactionRetrieverStep(_transactionRetriever, _logger),
                new CandidateBuilderStep(_producerUniqueId, _randomFactory, _hashProvider, _logger),
                new ProducedDeltaBuilderStep(_dateTimeProvider, _logger),
                new DeltaStateCalculationStep(_stateProvider, _deltaExecutor, _logger),
                new DeltaRegistrationStep(_deltaCache, _logger)
            };
        }

        IDeltaBuilderStep[] _steps;

        ///<inheritdoc />
        public CandidateDeltaBroadcast BuildCandidateDelta(Cid previousDeltaHash)
        {
            DeltaBuilderContext context = new DeltaBuilderContext(previousDeltaHash);
            foreach (IDeltaBuilderStep step in _steps)
            {
                step.Execute(context);
            }

            // to simplify cases when we test truffle
            // if ((context.Transactions?.Count ?? 0) == 0)
            // {
            //     return null;
            // }

            return context.Candidate;
        }
    }
}
