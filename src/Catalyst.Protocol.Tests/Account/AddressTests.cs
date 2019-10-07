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
using System.Linq;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Account;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Protocol.Tests.Account
{
    public class AddressTests
    {
        private readonly ITestOutputHelper _output;

        public AddressTests(ITestOutputHelper output) { _output = output; }

        //[Fact]
        //public void Address_should_produce_different_bytes_for_all_possible_network_and_account_types()
        //{
        //    var cartesianProduct = TestUtils.Protocol.AddressHelper.GetAllNetworksAndAccountTypesCombinations();

        //    var addressTypes = cartesianProduct.ToList();
        //    var forOutput = addressTypes.Select(x =>
        //        $"{Convert.ToString(((int) x.NetworkType | (int) x.AccountType), 2).PadLeft(6, '0')} =>  {x.NetworkType}|{x.AccountType}");
        //    forOutput.ToList().ForEach(o => _output.WriteLine(o));

        //    var pubKeyBytes = ByteUtil.GenerateRandomByteArray(new FfiWrapper().PublicKeyLength);

        //    var addressesFromSamePubkey = addressTypes.Select(t => new Address
        //    {
        //        PublicKeyHash = pubKeyBytes.ToByteString(),
        //        AccountType = t.AccountType,
        //        NetworkType = t.NetworkType
        //    }).ToList();

        //    addressesFromSamePubkey
        //       .Select(a => a.RawBytes.ToByteString().ToBase64()).Should().OnlyHaveUniqueItems();
        //}
    }
}

