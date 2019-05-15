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

namespace Catalyst.Common.Util
{
    /// <summary>
    ///     Original inspired from https://github.com/Nethereum/Nethereum
    /// </summary>
    public sealed class UnitConversion
    {
        private static UnitConversion _convert;

        public static UnitConversion Convert => _convert ?? (_convert = new UnitConversion());

        /// <summary>
        ///     Converts from Fulhame to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
        ///     significant digits
        /// </summary>
        public decimal FromFulhame(BigInteger value, BigInteger toUnit) => FromFulhame(value, GetKatUnitValueLength(toUnit));

        /// <summary>
        ///     Converts from Fulhame to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
        ///     significant digits
        /// </summary>
        public decimal FromFulhame(BigInteger value, string unitName = "Kat") => FromFulhame(value, Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt);

        /// <summary>
        ///     Converts from Fulhame to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
        ///     significant digits
        /// </summary>
        public static decimal FromFulhame(BigInteger value, int decimalPlacesToUnit) => (decimal) new BigDecimal(value, decimalPlacesToUnit * -1);

        public static BigDecimal FromFulhameToBigDecimal(BigInteger value, int decimalPlacesToUnit) => new BigDecimal(value, decimalPlacesToUnit * -1);

        public BigDecimal FromFulhameToBigDecimal(BigInteger value, string unitName = "Kat") => FromFulhameToBigDecimal(value, Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt);

        public BigDecimal FromFulhameToBigDecimal(BigInteger value, BigInteger toUnit) => FromFulhameToBigDecimal(value, GetKatUnitValueLength(toUnit));

        private static int GetKatUnitValueLength(BigInteger unitValue) => unitValue.ToString().Length - 1;

        public static bool TryValidateUnitValue(BigInteger katUnit)
        {
            if (katUnit.ToString().Trim('0') == "1")
            {
                return true;
            }
            
            throw new Exception("Invalid unit value, it should be a power of 10 ");
        }

        public BigInteger ToFulhameFromUnit(decimal amount, BigInteger fromUnit) => ToFulhameFromUnit((BigDecimal) amount, fromUnit);

        public BigInteger ToFulhameFromUnit(BigDecimal amount, BigInteger fromUnit)
        {
            TryValidateUnitValue(fromUnit);
            var bigDecimalFromUnit = new BigDecimal(fromUnit, 0);
            var conversion = amount * bigDecimalFromUnit;
            return conversion.Floor().Mantissa;
        }

        public BigInteger ToFulhame(BigDecimal amount, string unitName = "Kat") => ToFulhameFromUnit(amount, Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt);

        public BigInteger ToFulhame(BigDecimal amount, int decimalPlacesFromUnit)
        {
            if (decimalPlacesFromUnit == 0)
            {
                ToFulhame(amount, 1);
            }
            
            return ToFulhameFromUnit(amount, BigInteger.Pow(10, decimalPlacesFromUnit));
        }

        public BigInteger ToFulhame(decimal amount, int decimalPlacesFromUnit)
        {
            if (decimalPlacesFromUnit == 0)
            {
                ToFulhame(amount, 1);
            }
            
            return ToFulhameFromUnit(amount, BigInteger.Pow(10, decimalPlacesFromUnit));
        }

        public BigInteger ToFulhame(decimal amount, string unitName = "Kat") => ToFulhameFromUnit(amount, Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt);

        public static BigInteger ToFulhame(BigInteger value, string unitName = "Kat") => value * Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt;

        public BigInteger ToFulhame(int value, string unitName = "Kat") => ToFulhame(new BigInteger(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);

        public BigInteger ToFulhame(double value, string unitName = "Kat") => ToFulhame(System.Convert.ToDecimal(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);

        public BigInteger ToFulhame(float value, string unitName = "Kat") => ToFulhame(System.Convert.ToDecimal(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);

        public BigInteger ToFulhame(long value, string unitName = "Kat") => ToFulhame(new BigInteger(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);

        public BigInteger ToFulhame(string value, string unitName = "Kat") => ToFulhame(decimal.Parse(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);

        private BigInteger CalculateNumberOfDecimalPlaces(double value,
            int maxNumberOfDecimals,
            int currentNumberOfDecimals = 0) =>
            CalculateNumberOfDecimalPlaces(System.Convert.ToDecimal(value), maxNumberOfDecimals,
                currentNumberOfDecimals);

        private BigInteger CalculateNumberOfDecimalPlaces(float value,
            int maxNumberOfDecimals,
            int currentNumberOfDecimals = 0) =>
            CalculateNumberOfDecimalPlaces(System.Convert.ToDecimal(value), maxNumberOfDecimals,
                currentNumberOfDecimals);

        private static int CalculateNumberOfDecimalPlaces(decimal value,
            int maxNumberOfDecimals,
            int currentNumberOfDecimals = 0)
        {
            while (true)
            {
                if (currentNumberOfDecimals == 0)
                {
                    if (value.ToString() == Math.Round(value).ToString())
                    {
                        return 0;
                    }

                    currentNumberOfDecimals = 1;
                }

                if (currentNumberOfDecimals == maxNumberOfDecimals)
                {
                    return maxNumberOfDecimals;
                }

                var multiplied = value * (decimal) BigInteger.Pow(10, currentNumberOfDecimals);

                if (Math.Round(multiplied) == multiplied)
                {
                    return currentNumberOfDecimals;
                }
                
                currentNumberOfDecimals = currentNumberOfDecimals + 1;
            }
        }
    }
}
