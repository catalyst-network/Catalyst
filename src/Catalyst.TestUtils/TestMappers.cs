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

using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Cryptography;
using Catalyst.Core.Lib.DAO.Deltas;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.DAO.Peer;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Modules.Hashing;
using MultiFormats.Registry;

namespace Catalyst.TestUtils
{
    public class TestMapperProvider : MapperProvider
    {
        public TestMapperProvider() : base(new IMapperInitializer[]
        {
            new ProtocolMessageMapperInitialiser(),
            new ConfidentialEntryMapperInitialiser(),
            new CandidateDeltaBroadcastMapperInitialiser(),
            new ProtocolErrorMessageMapperInitialiser(),
            new PeerIdMapperInitialiser(),
            new SigningContextMapperInitialiser(),
            new DeltaMapperInitialiser(),
            new CandidateDeltaBroadcastMapperInitialiser(),
            new DeltaDfsHashBroadcastMapperInitialiser(),
            new FavouriteDeltaBroadcastMapperInitialiser(),
            new CoinbaseEntryMapperInitialiser(),
            new PublicEntryMapperInitialiser(new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"))),
            new ConfidentialEntryMapperInitialiser(),
            new TransactionBroadcastMapperInitialiser(),
            new SignatureMapperInitialiser(),
            new DeltaIndexMapperInitialiser()
        }) { }
    }
}
