﻿using System.Numerics;
using Catalyst.Node.Core.Helpers.Util;
using FluentAssertions;
using Nethereum.Hex.HexConvertors.Extensions;
using Xunit;

namespace Catalyst.Node.UnitTests.Helpers.Util
{
    public class ContractUtilTests
    {
        [Fact]
        public void ShouldCalculateContractAddress()
        {
            var adresss = "0x12890d2cce102216644c59daE5baed380d84830c";
            var nonce = 0;
            var expected = "0x243e72b69141f6af525a9a5fd939668ee9f2b354";
            var contractAddress = ContractUtils.CalculateContractAddress(adresss, new BigInteger(nonce));
            contractAddress.IsTheSameHex(expected).Should().BeTrue();
        }
    }
}