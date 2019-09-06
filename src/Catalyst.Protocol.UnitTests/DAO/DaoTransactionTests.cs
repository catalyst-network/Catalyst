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
using Catalyst.Core.Util;
using Catalyst.Protocol.DAO;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Protocol.UnitTests.DAO
{
    public class DaoTransactionTests
    {
        [Fact]
        public static void CoinbaseEntryDao_CoinbaseEntry_Should_Be_Convertible()
        {
            var coinbaseEntryDao = new CoinbaseEntryDao();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var message = new CoinbaseEntry
            {
                Version = 415,
                PubKey = byteRn.ToByteString(),
                Amount = 271314
            };

            var messageDao = coinbaseEntryDao.ToDao(message);
            var protoBuff = messageDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void STTransactionEntryDao_STTransactionEntry_Should_Be_Convertible()
        {
            var stTransactionEntryDao = new STTransactionEntryDao();
            var byteRn = new byte[30];
            new Random().NextBytes(byteRn);

            var message = new STTransactionEntry
            {
                PubKey = byteRn.ToByteString(),
                Amount = 8855274
            };

            var transactionEntryDao = stTransactionEntryDao.ToDao(message);
            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void CFTransactionEntryDao_CFTransactionEntry_Should_Be_Convertible()
        {
            var cfTransactionEntryDao = new CFTransactionEntryDao();
            var byteRn = new byte[30];
            var pedersenCommitBytes = new byte[50];

            var rnd = new Random();
            rnd.NextBytes(byteRn);
            rnd.NextBytes(pedersenCommitBytes);

            var message = new CFTransactionEntry
            {
                PubKey = byteRn.ToByteString(),
                PedersenCommit = pedersenCommitBytes.ToByteString()
            };

            var transactionEntryDao = cfTransactionEntryDao.ToDao(message);
            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }

        [Fact]
        public static void TransactionBroadcastDao_TransactionBroadcast_Should_Be_Convertible()
        {
            var transactionBroadcastDao = new TransactionBroadcastDao();

            var message = TransactionHelper.GetTransaction();

            var transactionEntryDao = transactionBroadcastDao.ToDao(message);
            var protoBuff = transactionEntryDao.ToProtoBuff();
            message.Should().Be(protoBuff);
        }
    }
}
