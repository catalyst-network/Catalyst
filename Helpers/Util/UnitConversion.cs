//using System;
//using System.Numerics;
//
//namespace ADL.Util
//{
//    public class UnitConversion
//    {
//        public enum AtlasUnit
//        {
//            Satoshi,
//            Joey,
//            Uzairi,
//            Brian,
//            Petie,
//            Robbie,
//            Fioi,
//            Konnie,
//            Stevie,
//            Franny,
//            Nicoli,
//            Paulie,
//            Toni,
//            Chrissy,
//            Eddie,
//            Danny,
//            Jimi,
//            Dollar,
//            Kypros,
//            Amit,
//            Grand,
//            Casu,
//            Vahan,
//            Tether
//        }
//
//        private static UnitConversion _convert;
//
//        public static UnitConversion Convert => _convert ?? (_convert = new UnitConversion());
//
//        /// <summary>
//        ///     Converts from wei to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
//        ///     significant digits
//        /// </summary>
//        public decimal FromWei(BigInteger value, BigInteger toUnit)
//        {
//            return FromWei(value, GetAtlasUnitValueLength(toUnit));
//        }
//
//        /// <summary>
//        ///     Converts from wei to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
//        ///     significant digits
//        /// </summary>
//        public decimal FromWei(BigInteger value, AtlasUnit toUnit = AtlasUnit.Atlaser)
//        {
//            return FromWei(value, GetAtlasUnitValue(toUnit));
//        }
//
//        /// <summary>
//        ///     Converts from wei to a unit, NOTE: When the total number of digits is bigger than 29 they will be rounded the less
//        ///     significant digits
//        /// </summary>
//        public decimal FromWei(BigInteger value, int decimalPlacesToUnit)
//        {
//            return (decimal) new BigDecimal(value, decimalPlacesToUnit * -1);
//        }
//
//        public BigDecimal FromWeiToBigDecimal(BigInteger value, int decimalPlacesToUnit)
//        {
//            return new BigDecimal(value, decimalPlacesToUnit * -1);
//        }
//
//        public BigDecimal FromWeiToBigDecimal(BigInteger value, AtlasUnit toUnit = AtlasUnit.Atlaser)
//        {
//            return FromWeiToBigDecimal(value, GetAtlasUnitValue(toUnit));
//        }
//
//        public BigDecimal FromWeiToBigDecimal(BigInteger value, BigInteger toUnit)
//        {
//            return FromWeiToBigDecimal(value, GetAtlasUnitValueLength(toUnit));
//        }
//
//        private int GetAtlasUnitValueLength(BigInteger unitValue)
//        {
//            return unitValue.ToString().Length - 1;
//        }
//
//        private BigInteger GetAtlasUnitValue(AtlasUnit ethUnit)
//        {
//            switch (ethUnit)
//            {
//                case AtlasUnit.Wei:
//                    return BigInteger.Parse("1");
//                case AtlasUnit.Kwei:
//                    return BigInteger.Parse("1000");
//                case AtlasUnit.Babbage:
//                    return BigInteger.Parse("1000");
//                case AtlasUnit.Femtoether:
//                    return BigInteger.Parse("1000");
//                case AtlasUnit.Mwei:
//                    return BigInteger.Parse("1000000");
//                case AtlasUnit.Picoether:
//                    return BigInteger.Parse("1000000");
//                case AtlasUnit.Gwei:
//                    return BigInteger.Parse("1000000000");
//                case AtlasUnit.Shannon:
//                    return BigInteger.Parse("1000000000");
//                case AtlasUnit.Nanoether:
//                    return BigInteger.Parse("1000000000");
//                case AtlasUnit.Nano:
//                    return BigInteger.Parse("1000000000");
//                case AtlasUnit.Szabo:
//                    return BigInteger.Parse("1000000000000");
//                case AtlasUnit.Microether:
//                    return BigInteger.Parse("1000000000000");
//                case AtlasUnit.Micro:
//                    return BigInteger.Parse("1000000000000");
//                case AtlasUnit.Finney:
//                    return BigInteger.Parse("1000000000000000");
//                case AtlasUnit.Milliether:
//                    return BigInteger.Parse("1000000000000000");
//                case AtlasUnit.jimi:
//                    return BigInteger.Parse("1000000000000000");
//                case AtlasUnit.Dollar:
//                    return BigInteger.Parse("1000000000000000000");
//                case AtlasUnit.Kether:
//                    return BigInteger.Parse("1000000000000000000000");
//                case AtlasUnit.Grand:
//                    return BigInteger.Parse("1000000000000000000000");
//                case AtlasUnit.Einstein:
//                    return BigInteger.Parse("1000000000000000000000");
//                case AtlasUnit.Mether:
//                    return BigInteger.Parse("1000000000000000000000000");
//                case AtlasUnit.Gether:
//                    return BigInteger.Parse("1000000000000000000000000000");
//                case AtlasUnit.Tether:
//                    return BigInteger.Parse("1000000000000000000000000000000");
//            }
//            throw new NotImplementedException();
//        }
//
//        public bool TryValidateUnitValue(BigInteger ethUnit)
//        {
//            if (ethUnit.ToString().Trim('0') == "1") return true;
//            throw new ArgumentOutOfRangeException("Invalid unit value, it should be a power of 10 ");
//        }
//
//        public BigInteger ToWeiFromUnit(decimal amount, BigInteger fromUnit)
//        {
//            return ToWeiFromUnit((BigDecimal) amount, fromUnit);
//        }
//
//        public BigInteger ToWeiFromUnit(BigDecimal amount, BigInteger fromUnit)
//        {
//            TryValidateUnitValue(fromUnit);
//            var bigDecimalFromUnit = new BigDecimal(fromUnit, 0);
//            var conversion = amount * bigDecimalFromUnit;
//            return conversion.Floor().Mantissa;
//        }
//
//        public BigInteger ToWei(BigDecimal amount, AtlasUnit fromUnit = AtlasUnit.Atlaser)
//        {
//            return ToWeiFromUnit(amount, GetAtlasUnitValue(fromUnit));
//        }
//
//        public BigInteger ToWei(BigDecimal amount, int decimalPlacesFromUnit)
//        {
//            if (decimalPlacesFromUnit == 0) ToWei(amount, 1);
//            return ToWeiFromUnit(amount, BigInteger.Pow(10, decimalPlacesFromUnit));
//        }
//
//        public BigInteger ToWei(decimal amount, int decimalPlacesFromUnit)
//        {
//            if (decimalPlacesFromUnit == 0) ToWei(amount, 1);
//            return ToWeiFromUnit(amount, BigInteger.Pow(10, decimalPlacesFromUnit));
//        }
//
//        public BigInteger ToWei(decimal amount, AtlasUnit fromUnit = AtlasUnit.Atlaser)
//        {
//            return ToWeiFromUnit(amount, GetAtlasUnitValue(fromUnit));
//        }
//
//        public BigInteger ToWei(BigInteger value, AtlasUnit fromUnit = AtlasUnit.Atlaser)
//        {
//            return value * GetAtlasUnitValue(fromUnit);
//        }
//
//        public BigInteger ToWei(int value, AtlasUnit fromUnit = AtlasUnit.Atlaser)
//        {
//            return ToWei(new BigInteger(value), fromUnit);
//        }
//
//        public BigInteger ToWei(double value, AtlasUnit fromUnit = AtlasUnit.Atlaser)
//        {
//            return ToWei(System.Convert.ToDecimal(value), fromUnit);
//        }
//
//        public BigInteger ToWei(float value, AtlasUnit fromUnit = AtlasUnit.Atlaser)
//        {
//            return ToWei(System.Convert.ToDecimal(value), fromUnit);
//        }
//
//        public BigInteger ToWei(long value, AtlasUnit fromUnit = AtlasUnit.Atlaser)
//        {
//            return ToWei(new BigInteger(value), fromUnit);
//        }
//
//        public BigInteger ToWei(string value, AtlasUnit fromUnit = AtlasUnit.Atlaser)
//        {
//            return ToWei(decimal.Parse(value), fromUnit);
//        }
//
//        private BigInteger CalculateNumberOfDecimalPlaces(double value, int maxNumberOfDecimals,
//            int currentNumberOfDecimals = 0)
//        {
//            return CalculateNumberOfDecimalPlaces(System.Convert.ToDecimal(value), maxNumberOfDecimals,
//                currentNumberOfDecimals);
//        }
//
//        private BigInteger CalculateNumberOfDecimalPlaces(float value, int maxNumberOfDecimals,
//            int currentNumberOfDecimals = 0)
//        {
//            return CalculateNumberOfDecimalPlaces(System.Convert.ToDecimal(value), maxNumberOfDecimals,
//                currentNumberOfDecimals);
//        }
//
//        private int CalculateNumberOfDecimalPlaces(decimal value, int maxNumberOfDecimals,
//            int currentNumberOfDecimals = 0)
//        {
//            if (currentNumberOfDecimals == 0)
//            {
//                if (value.ToString() == Math.Round(value).ToString()) return 0;
//                currentNumberOfDecimals = 1;
//            }
//            if (currentNumberOfDecimals == maxNumberOfDecimals) return maxNumberOfDecimals;
//            var multiplied = value * (decimal) BigInteger.Pow(10, currentNumberOfDecimals);
//            if (Math.Round(multiplied) == multiplied)
//                return currentNumberOfDecimals;
//            return CalculateNumberOfDecimalPlaces(value, maxNumberOfDecimals, currentNumberOfDecimals + 1);
//        }
//
//        //public BigInteger ToWei(decimal amount, BigInteger fromUnit)
//        //{
//
//        //var maxDigits = fromUnit.ToString().Length - 1;
//        //var stringAmount = amount.ToString("#.#############################", System.Globalization.CultureInfo.InvariantCulture);
//        //if (stringAmount.IndexOf(".") == -1)
//        //{
//        //    return BigInteger.Parse(stringAmount) * fromUnit;
//        //}
//
//        //stringAmount = stringAmount.TrimEnd('0');
//        //var decimalPosition = stringAmount.IndexOf('.');
//        //var decimalPlaces = decimalPosition == -1 ? 0 : stringAmount.Length - decimalPosition - 1;
//
//
//        //if (decimalPlaces > maxDigits)
//        //{
//        //    return BigInteger.Parse(stringAmount.Substring(0, decimalPosition) + stringAmount.Substring(decimalPosition + 1, maxDigits));
//        //}
//
//        //return BigInteger.Parse(stringAmount.Substring(0, decimalPosition) + stringAmount.Substring(decimalPosition + 1).PadRight(maxDigits, '0'));   
//        //}
//    }
//}
