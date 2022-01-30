#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Consensus;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Google.Protobuf.WellKnownTypes;
using Serilog;
using Serilog.Events;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class ProducedDeltaBuilderStep : IDeltaBuilderStep
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;

        public ProducedDeltaBuilderStep(IDateTimeProvider dateTimeProvider, ILogger logger)
        {
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Execute(DeltaBuilderContext context)
        {
            context.ProducedDelta = new Delta
            {
                PreviousDeltaDfsHash = context.PreviousDeltaHash.ToArray().ToByteString(),
                DeltaNumber = context.PreviousDelta.DeltaNumber + 1,
                MerkleRoot = context.Candidate.Hash,
                CoinbaseEntries =
                {
                    context.CoinbaseEntry
                },

                PublicEntries =
                {
                    context.Transactions
                },

                GasUsed = (long)context.Transactions.Sum(x => x.GasLimit),

                TimeStamp = Timestamp.FromDateTime(_dateTimeProvider.UtcNow)
            };

            if (_logger.IsEnabled(LogEventLevel.Information)) _logger.Information("Built a delta {number} {hash} based on {previousHash} with {txCount} transactions", context.ProducedDelta.DeltaNumber, context.Candidate.Hash, context.ProducedDelta.PreviousDeltaDfsHash, context.ProducedDelta.PublicEntries.Count);
            foreach (PublicEntry publicEntry in context.ProducedDelta.PublicEntries)
            {
                if (_logger.IsEnabled(LogEventLevel.Information)) _logger.Information("Entry {from} -> {to} with nonce {nonce}", publicEntry.SenderAddress.ToAddress(), publicEntry.ReceiverAddress.ToAddress(), publicEntry.Nonce);
            }
        }
    }
}
