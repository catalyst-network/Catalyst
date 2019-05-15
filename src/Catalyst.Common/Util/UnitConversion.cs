using System;
using System.Numerics;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;

namespace Catalyst.Common.Util
{
    public sealed class UnitConversion
    {
        private static UnitConversion _convert;

        public static UnitConversion Convert => _convert ?? (_convert = new UnitConversion());

        /// <summary>
        ///     Converts from wei to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
        ///     significant digits
        /// </summary>
        public decimal FromFulhame(BigInteger value, BigInteger toUnit)
        {
            return FromFulhame(value, GetKatUnitValueLength(toUnit));
        }

        /// <summary>
        ///     Converts from wei to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
        ///     significant digits
        /// </summary>
        public decimal FromFulhame(BigInteger value, string unitName = "Kat")
        {
            return FromFulhame(value, Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt);
        }

        /// <summary>
        ///     Converts from wei to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
        ///     significant digits
        /// </summary>
        public decimal FromFulhame(BigInteger value, int decimalPlacesToUnit)
        {
            return (decimal) new BigDecimal(value, decimalPlacesToUnit * -1);
        }

        public BigDecimal FromFulhameToBigDecimal(BigInteger value, int decimalPlacesToUnit)
        {
            return new BigDecimal(value, decimalPlacesToUnit * -1);
        }

        public BigDecimal FromFulhameToBigDecimal(BigInteger value, string unitName = "Kat")
        {
            return FromFulhameToBigDecimal(value, Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt);
        }

        public BigDecimal FromFulhameToBigDecimal(BigInteger value, BigInteger toUnit)
        {
            return FromFulhameToBigDecimal(value, GetKatUnitValueLength(toUnit));
        }

        private int GetKatUnitValueLength(BigInteger unitValue)
        {
            return unitValue.ToString().Length - 1;
        }

        public bool TryValidateUnitValue(BigInteger katUnit)
        {
            if (katUnit.ToString().Trim('0') == "1")
            {
                return true;
            }
            
            throw new Exception("Invalid unit value, it should be a power of 10 ");
        }

        public BigInteger ToFulhameFromUnit(decimal amount, BigInteger fromUnit)
        {
            return ToFulhameFromUnit((BigDecimal) amount, fromUnit);
        }

        public BigInteger ToFulhameFromUnit(BigDecimal amount, BigInteger fromUnit)
        {
            TryValidateUnitValue(fromUnit);
            var bigDecimalFromUnit = new BigDecimal(fromUnit, 0);
            var conversion = amount * bigDecimalFromUnit;
            return conversion.Floor().Mantissa;
        }

        public BigInteger ToFulhame(BigDecimal amount, string unitName = "Kat")
        {
            return ToFulhameFromUnit(amount, Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt);
        }

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

        public BigInteger ToFulhame(decimal amount, string unitName = "Kat")
        {
            return ToFulhameFromUnit(amount, Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt);
        }

        public BigInteger ToFulhame(BigInteger value, string unitName = "Kat")
        {
            return value * Enumeration.Parse<UnitTypes>(unitName).UnitAsBigInt;
        }

        public BigInteger ToFulhame(int value, string unitName = "Kat")
        {
            return ToFulhame(new BigInteger(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);
        }

        public BigInteger ToFulhame(double value, string unitName = "Kat")
        {
            return ToFulhame(System.Convert.ToDecimal(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);
        }

        public BigInteger ToFulhame(float value, string unitName = "Kat")
        {
            return ToFulhame(System.Convert.ToDecimal(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);
        }

        public BigInteger ToFulhame(long value, string unitName = "Kat")
        {
            return ToFulhame(new BigInteger(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);
        }

        public BigInteger ToFulhame(string value, string unitName = "Kat")
        {
            return ToFulhame(decimal.Parse(value), Enumeration.Parse<UnitTypes>(unitName).UnitString);
        }

        private BigInteger CalculateNumberOfDecimalPlaces(double value,
            int maxNumberOfDecimals,
            int currentNumberOfDecimals = 0)
        {
            return CalculateNumberOfDecimalPlaces(System.Convert.ToDecimal(value), maxNumberOfDecimals,
                currentNumberOfDecimals);
        }

        private BigInteger CalculateNumberOfDecimalPlaces(float value,
            int maxNumberOfDecimals,
            int currentNumberOfDecimals = 0)
        {
            return CalculateNumberOfDecimalPlaces(System.Convert.ToDecimal(value), maxNumberOfDecimals,
                currentNumberOfDecimals);
        }

        private int CalculateNumberOfDecimalPlaces(decimal value,
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
