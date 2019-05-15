using System;
using System.Numerics;

namespace Catalyst.Common.Util
{
    public sealed class UnitConversion
    {
        public enum KatUnit
        {
            Fulhame,
            KitKat,
            GrumpyFace,
            GarryLazerEyes,
            SteveFrench,
            YoctoKat,
            ZeptoKat,
            AtooKat,
            FemtoKat,
            PicoKat,
            NanoKat,
            MicroKat,
            MiliKat,
            CentiKat,
            Korin,
            DekaKat,
            HectoKat,
            Kat,
            MegaKat,
            GigaKat,
            Schrodinger,
            TerraKat,
            PetaKat,
            FatKat
        }

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
        public decimal FromFulhame(BigInteger value, KatUnit toUnit = KatUnit.Kat)
        {
            return FromFulhame(value, GetKatUnitValue(toUnit));
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

        public BigDecimal FromFulhameToBigDecimal(BigInteger value, KatUnit toUnit = KatUnit.Kat)
        {
            return FromFulhameToBigDecimal(value, GetKatUnitValue(toUnit));
        }

        public BigDecimal FromFulhameToBigDecimal(BigInteger value, BigInteger toUnit)
        {
            return FromFulhameToBigDecimal(value, GetKatUnitValueLength(toUnit));
        }

        private int GetKatUnitValueLength(BigInteger unitValue)
        {
            return unitValue.ToString().Length - 1;
        }

        public BigInteger GetKatUnitValue(KatUnit katUnit)
        {
            switch (katUnit)
            {
                case KatUnit.Fulhame:
                    return BigInteger.Parse("1");
                case KatUnit.KitKat:
                    return BigInteger.Parse("1000");
                case KatUnit.GrumpyFace:
                    return BigInteger.Parse("1000");
                case KatUnit.YoctoKat:
                    return BigInteger.Parse("1000");
                case KatUnit.GarryLazerEyes:
                    return BigInteger.Parse("1000");
                case KatUnit.SteveFrench:
                    return BigInteger.Parse("1000000");
                case KatUnit.ZeptoKat:
                    return BigInteger.Parse("1000000");
                case KatUnit.AtooKat:
                    return BigInteger.Parse("1000000000");
                case KatUnit.FemtoKat:
                    return BigInteger.Parse("1000000000");
                case KatUnit.PicoKat:
                    return BigInteger.Parse("1000000000");
                case KatUnit.NanoKat:
                    return BigInteger.Parse("1000000000");
                case KatUnit.MicroKat:
                    return BigInteger.Parse("1000000000000");
                case KatUnit.MiliKat:
                    return BigInteger.Parse("1000000000000");
                case KatUnit.CentiKat:
                    return BigInteger.Parse("1000000000000");
                case KatUnit.Korin:
                    return BigInteger.Parse("1000000000000000");
                case KatUnit.DekaKat:
                    return BigInteger.Parse("1000000000000000");
                case KatUnit.HectoKat:
                    return BigInteger.Parse("1000000000000000");
                case KatUnit.Kat:
                    return BigInteger.Parse("1000000000000000000");
                case KatUnit.MegaKat:
                    return BigInteger.Parse("1000000000000000000000");
                case KatUnit.GigaKat:
                    return BigInteger.Parse("1000000000000000000000");
                case KatUnit.Schrodinger:
                    return BigInteger.Parse("1000000000000000000000");
                case KatUnit.TerraKat:
                    return BigInteger.Parse("1000000000000000000000000");
                case KatUnit.PetaKat:
                    return BigInteger.Parse("1000000000000000000000000000");
                case KatUnit.FatKat:
                    return BigInteger.Parse("1000000000000000000000000000000");
                default:
                    throw new ArgumentOutOfRangeException(nameof(katUnit), katUnit, "I haz no units for you");
            }
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

        public BigInteger ToFulhame(BigDecimal amount, KatUnit fromUnit = KatUnit.Kat)
        {
            return ToFulhameFromUnit(amount, GetKatUnitValue(fromUnit));
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

        public BigInteger ToFulhame(decimal amount, KatUnit fromUnit = KatUnit.Kat)
        {
            return ToFulhameFromUnit(amount, GetKatUnitValue(fromUnit));
        }

        public BigInteger ToFulhame(BigInteger value, KatUnit fromUnit = KatUnit.Kat)
        {
            return value * GetKatUnitValue(fromUnit);
        }

        public BigInteger ToFulhame(int value, KatUnit fromUnit = KatUnit.Kat)
        {
            return ToFulhame(new BigInteger(value), fromUnit);
        }

        public BigInteger ToFulhame(double value, KatUnit fromUnit = KatUnit.Kat)
        {
            return ToFulhame(System.Convert.ToDecimal(value), fromUnit);
        }

        public BigInteger ToFulhame(float value, KatUnit fromUnit = KatUnit.Kat)
        {
            return ToFulhame(System.Convert.ToDecimal(value), fromUnit);
        }

        public BigInteger ToFulhame(long value, KatUnit fromUnit = KatUnit.Kat)
        {
            return ToFulhame(new BigInteger(value), fromUnit);
        }

        public BigInteger ToFulhame(string value, KatUnit fromUnit = KatUnit.Kat)
        {
            return ToFulhame(decimal.Parse(value), fromUnit);
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
