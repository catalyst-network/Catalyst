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

using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO;

namespace Catalyst.TestUtils
{
    public static class TestMappers
    {
        public static void Start()
        {
            var mappers = new IMapperInitializer[]
            {
                new ProtocolMessageDao(),
                new ConfidentialEntryDao(),
                new ProtocolErrorMessageSignedDao(),
                new PeerIdDao(),
                new SigningContextDao(),
                new CoinbaseEntryDao(),
                new PublicEntryDao(),
                new ConfidentialEntryDao(),
                new TransactionBroadcastDao(),
                new ContractEntryDao(),
                new SignatureDao(),
                new BaseEntryDao()
            };

            var map = new MapperProvider(mappers);
            map.Start();
        }
    }
}
