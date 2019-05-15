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
using System.Numerics;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Util;
using Xunit;

namespace Catalyst.Common.UnitTests.Utils
{
    /// <summary>
    ///     Original inspired from https://github.com/Nethereum/Nethereum
    /// </summary>
    public sealed class ConversionTests
    {
        [Theory]
        [InlineData(18, "1111111111.111111111111111111", "1111111111111111111111111111")]
        [InlineData(18, "11111111111.111111111111111111", "11111111111111111111111111111")]
        
        //Rounding happens when having more than 29 digits
        [InlineData(18, "111111111111.11111111111111111", "111111111111111111111111111111")]
        [InlineData(18, "1111111111111.1111111111111111", "1111111111111111111111111111111")]
        public void ShouldConvertFromFulhameToDecimal(int units, string expected, string fulhameAmount)
        {
            var unitConversion = new UnitConversion();
            var result = UnitConversion.FromFulhame(BigInteger.Parse(fulhameAmount), units);
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [InlineData(18, "1111111111.111111111111111111", "1111111111111111111111111111")]
        [InlineData(18, "11111111111.111111111111111111", "11111111111111111111111111111")]
        [InlineData(18, "111111111111.111111111111111111", "111111111111111111111111111111")]
        [InlineData(18, "1111111111111.111111111111111111", "1111111111111111111111111111111")]
        [InlineData(30, "1111111111111111111.111111111111111111111111111111",
            "1111111111111111111111111111111111111111111111111")]
        public void ShouldConvertFromFulhameToBigDecimal(int units, string expected, string fulhameAmount)
        {
            var unitConversion = new UnitConversion();
            var result = UnitConversion.FromFulhameToBigDecimal(BigInteger.Parse(fulhameAmount), units);
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void ShouldFailToConvertUsingNonPowerOf10Units(int value)
        {
            var unitConversion = new UnitConversion();
            var val = BigInteger.Parse("1000000000000000000000000001");
            var result = UnitConversion.FromFulhame(val, 18);
            Assert.Throws<Exception>(() => UnitConversion.Convert.ToFulhameFromUnit(result, new BigInteger(value)));
        }

        [Fact]
        public void ShouldConvertFromFulhame()
        {
            var unitConversion = new UnitConversion();
            var val = BigInteger.Parse("1000000000000000000000000001");

            var result = UnitConversion.FromFulhame(val, 18);
            var result2 = unitConversion.FromFulhame(val);
            Assert.Equal(result, result2);
            result2 = unitConversion.FromFulhame(val, BigInteger.Parse("1000000000000000000"));
            Assert.Equal(result, result2);
            Assert.Equal(val, UnitConversion.Convert.ToFulhame(result));
        }

        [Fact]
        public void ShouldConvertFromDecimalUnit()
        {
            var unitConversion = new UnitConversion();
            const decimal kat = 0.0010m;
            var fulhame = UnitConversion.Convert.ToFulhame(kat, Enumeration.Parse<UnitTypes>("Kat").Name);
            var val = BigInteger.Parse("1".PadRight(16, '0'));
            var result = UnitConversion.FromFulhame(val, 18);
            Assert.Equal(UnitConversion.Convert.ToFulhame(result), fulhame);
        }

        [Fact]
        public void ShouldConvertFromFulhameAndBackToKat()
        {
            var unitConversion = new UnitConversion();
            var val = BigInteger.Parse("1000000000000000000000000001");
            var result = UnitConversion.FromFulhame(val, 18);
            var result2 = unitConversion.FromFulhame(val);
            Assert.Equal(result, result2);
            result2 = unitConversion.FromFulhame(val, BigInteger.Parse("1000000000000000000"));
            Assert.Equal(result, result2);
            Assert.Equal(val, UnitConversion.Convert.ToFulhame(result));
        }

        [Fact]
        public void ShouldConvertLargeDecimal()
        {
            var unitConversion = new UnitConversion();
            const decimal kat = 1.243842387924387924897423897423m;
            var fulhame = UnitConversion.Convert.ToFulhame(kat, Enumeration.Parse<UnitTypes>("Kat").Name);
            var val = BigInteger.Parse("1243842387924387924");
            var result = UnitConversion.FromFulhame(val, 18);
            Assert.Equal(UnitConversion.Convert.ToFulhame(result), fulhame);
        }

        [Fact]
        public void ShouldConvertNoDecimal()
        {
            var unitConversion = new UnitConversion();
            const decimal kat = 1m;
            var fuhame = UnitConversion.Convert.ToFulhame(kat, Enumeration.Parse<UnitTypes>("Kat").Name);
            var val = BigInteger.Parse("1".PadRight(19, '0'));
            var result = UnitConversion.FromFulhame(val, 18);
            Assert.Equal(UnitConversion.Convert.ToFulhame(result), fuhame);
        }

        [Fact]
        public void ShouldConvertNoDecimalIn10s()
        {
            var unitConversion = new UnitConversion();
            const decimal kat = 10m;
            var fulhame = UnitConversion.Convert.ToFulhame(kat, Enumeration.Parse<UnitTypes>("Kat").Name);
            var val = BigInteger.Parse("1".PadRight(20, '0'));
            var result = UnitConversion.FromFulhame(val, 18);
            Assert.Equal(UnitConversion.Convert.ToFulhame(result), fulhame);
        }

        [Fact]
        public void ShouldConvertPeriodic()
        {
            var unitConversion = new UnitConversion();
            const decimal kat = (decimal) 1 / 3;
            var fulhame = UnitConversion.Convert.ToFulhame(kat, Enumeration.Parse<UnitTypes>("Kat").Name);
            var val = BigInteger.Parse("333333333333333333");
            var result = UnitConversion.FromFulhame(val, 18);
            Assert.Equal(UnitConversion.Convert.ToFulhame(result), fulhame);
        }

        [Fact]
        public void ShouldConvertPeriodicPetaKat()
        {
            var unitConversion = new UnitConversion();
            const decimal kat = (decimal) 1 / 3;
            var fulhame = UnitConversion.Convert.ToFulhame(kat, Enumeration.Parse<UnitTypes>("PetaKat").Name);
            var val = BigInteger.Parse("3".PadLeft(27, '3'));
            var result = unitConversion.FromFulhame(val, Enumeration.Parse<UnitTypes>("PetaKat").Name);
            Assert.Equal(UnitConversion.Convert.ToFulhame(result, Enumeration.Parse<UnitTypes>("PetaKat").Name), fulhame);
        }

        [Fact]
        public void ShouldConvertPeriodicFatKat()
        {
            var unitConversion = new UnitConversion();
            var kat = new BigDecimal(1) / new BigDecimal(3);
            var fulhame = UnitConversion.Convert.ToFulhame(kat, Enumeration.Parse<UnitTypes>("FatKat").Name);
            var val = BigInteger.Parse("3".PadLeft(30, '3'));
            var result = unitConversion.FromFulhameToBigDecimal(val, Enumeration.Parse<UnitTypes>("FatKat").Name);
            Assert.Equal(UnitConversion.Convert.ToFulhame(result, Enumeration.Parse<UnitTypes>("FatKat").Name), fulhame);
        }

        [Fact]
        public void ShouldConvertSmallDecimal()
        {
            var unitConversion = new UnitConversion();
            const decimal kat = 1.24384m;
            var fulhame = UnitConversion.Convert.ToFulhame(kat, Enumeration.Parse<UnitTypes>("Kat").Name);
            var val = BigInteger.Parse("124384".PadRight(19, '0'));
            var result = UnitConversion.FromFulhame(val, 18);
            Assert.Equal(UnitConversion.Convert.ToFulhame(result), fulhame);
        }

        [Fact]
        public void ShouldConvertToFulhameUsingNumberOfDecimalsKat()
        {
            var unitConversion = new UnitConversion();
            var val = BigInteger.Parse("1000000000000000000000000001");
            var result = UnitConversion.FromFulhame(val, 18);
            var result2 = unitConversion.FromFulhame(val);
            Assert.Equal(result, result2);
            result2 = unitConversion.FromFulhame(val, BigInteger.Parse("1000000000000000000"));
            Assert.Equal(result, result2);
            Assert.Equal(val, UnitConversion.Convert.ToFulhame(result, 18));
        }

        [Fact]
        public void ShouldNotFailToConvertUsing0DecimalUnits()
        {
            var unitConversion = new UnitConversion();
            var val = BigInteger.Parse("1000000000000000000000000001");
            var result = UnitConversion.FromFulhame(val, 18);
            Assert.Equal(1000000000, UnitConversion.Convert.ToFulhame(result, 0));
        }

        [Fact]
        public void TrimmingOf0sShouldOnlyHappenForDecimalValues()
        {
            var unitConversion = new UnitConversion();
            var result1 = unitConversion.ToFulhame(10m);
            var result2 = unitConversion.ToFulhame(100m);
            Assert.NotEqual(result1.ToString(), result2.ToString());
        }
    }
}
