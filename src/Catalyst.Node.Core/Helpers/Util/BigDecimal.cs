using System;
using System.Globalization;
using System.Numerics;

namespace Catalyst.Node.Core.Helpers.Util
{
    /// BigNumber based on the original http://uberscraper.blogspot.co.uk/2013/09/c-bigdecimal-class-from-stackoverflow.html
    /// which was inspired by http://stackoverflow.com/a/4524254
    /// <summary>
    ///     Arbitrary precision Decimal.
    ///     All operations are exact, except for division.
    ///     Division never determines more digits than the given precision of 50.
    /// </summary>
    public struct BigDecimal : IComparable, IComparable<BigDecimal>
    {
        private int Exponent { get; set; }
        public BigInteger Mantissa { get; private set; }

        /// <summary>
        ///     Sets the maximum precision of division operations.
        ///     If AlwaysTruncate is set to true all operations are affected.
        /// </summary>
        private const int Precision = 50;

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="bigDecimal"></param>
        /// <param name="alwaysTruncate"></param>
        private BigDecimal(BigDecimal bigDecimal, bool alwaysTruncate = false) : this(bigDecimal.Mantissa,
            bigDecimal.Exponent, alwaysTruncate) { }

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="value"></param>
        /// <param name="alwaysTruncate"></param>
        public BigDecimal(decimal value, bool alwaysTruncate = false) : this((BigDecimal) value, alwaysTruncate) { }

        /// <summary>
        /// </summary>
        /// <param name="mantissa"></param>
        /// <param name="exponent">
        ///     @TODO Guart Util
        ///     The number of decimal units for example (-18). A positive value will be normalised as 10 ^
        ///     exponent
        /// </param>
        /// <param name="alwaysTruncate">
        ///     Specifies whether the significant digits should be truncated to the given precision after
        ///     each operation.
        /// </param>
        public BigDecimal(BigInteger mantissa, int exponent, bool alwaysTruncate = false) : this()
        {
            Mantissa = mantissa;
            Exponent = exponent;
            NormaliseExponentBiggerThanZero();
            Normalize();
            if (alwaysTruncate)
                Truncate();
        }

        /// <summary>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public int CompareTo(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj)); //@TODO Guart Util
            if (!(obj is BigDecimal))
                throw new ArgumentException();
            return CompareTo((BigDecimal) obj);
        }

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(BigDecimal other)
        {
            return this < other ? -1 : this > other ? 1 : 0;
        }

        /// <summary>
        /// </summary>
        private void NormaliseExponentBiggerThanZero()
        {
            if (Exponent > 0)
            {
                Mantissa = Mantissa * BigInteger.Pow(10, Exponent);
                Exponent = 0;
            }
        }

        /// <summary>
        ///     Removes trailing zeros on the mantissa
        /// </summary>
        private void Normalize()
        {
            if (Exponent == 0) return;

            if (Mantissa.IsZero)
            {
                Exponent = 0;
            }
            else
            {
                BigInteger remainder = 0;
                while (remainder == 0)
                {
                    var shortened = BigInteger.DivRem(Mantissa, 10, out remainder);
                    if (remainder != 0)
                        continue;
                    Mantissa = shortened;
                    Exponent++;
                }

                NormaliseExponentBiggerThanZero();
            }
        }

        /// <summary>
        ///     Truncate the number to the given precision by removing the least significant digits.
        /// </summary>
        /// <returns>The truncated number</returns>
        private BigDecimal Truncate(int precision = Precision)
        {
            // copy this instance (remember its a struct)
            var shortened = this;
            // save some time because the number of digits is not needed to remove trailing zeros
            shortened.Normalize();
            // remove the least significant digits, as long as the number of digits is higher than the given Precision
            while (shortened.Mantissa.NumberOfDigits() > precision)
            {
                shortened.Mantissa /= 10;
                shortened.Exponent++;
            }

            return shortened;
        }

        /// <summary>
        ///     Truncate the number, removing all decimal digits.
        /// </summary>
        /// <returns>The truncated number</returns>
        public BigDecimal Floor()
        {
            return Truncate(Mantissa.NumberOfDigits() + Exponent);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int NumberOfDigits(BigInteger value)
        {
            return value.NumberOfDigits();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            Normalize();
            var s = Mantissa.ToString();
            if (Exponent != 0)
            {
                var decimalPos = s.Length + Exponent;
                if (decimalPos < s.Length)
                    if (decimalPos >= 0)
                        s = s.Insert(decimalPos, decimalPos == 0 ? "0." : ".");
                    else
                        s = "0." + s.PadLeft(decimalPos * -1 + s.Length, '0');
                else
                    s = s.PadRight(decimalPos, '0');
            }

            return s;
        }

        /// <summary>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool Equals(BigDecimal other)
        {
            var first = this;
            var second = other;
            first.Normalize();
            second.Normalize();
            return second.Mantissa.Equals(first.Mantissa) && second.Exponent == first.Exponent;
        }

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return obj is BigDecimal a && Equals(a);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Mantissa.GetHashCode() * 397) ^ Exponent;
            }
        }

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator BigDecimal(int value)
        {
            return new BigDecimal(value, 0);
        }

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator BigDecimal(double value)
        {
            var mantissa = (BigInteger) value;
            var exponent = 0;
            double scaleFactor = 1;
            while (System.Math.Abs(value * scaleFactor - (double) mantissa) > 0)
            {
                exponent -= 1;
                scaleFactor *= 10;
                mantissa = (BigInteger) (value * scaleFactor);
            }

            return new BigDecimal(mantissa, exponent);
        }

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator BigDecimal(decimal value)
        {
            var mantissa = (BigInteger) value;
            var exponent = 0;
            decimal scaleFactor = 1;
            while ((decimal) mantissa != value * scaleFactor)
            {
                exponent -= 1;
                scaleFactor *= 10;
                mantissa = (BigInteger) (value * scaleFactor);
            }

            return new BigDecimal(mantissa, exponent);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static explicit operator double(BigDecimal value)
        {
            return double.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static explicit operator float(BigDecimal value)
        {
            return float.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static explicit operator decimal(BigDecimal value)
        {
            return decimal.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static explicit operator int(BigDecimal value)
        {
            return Convert.ToInt32((decimal) value);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static explicit operator uint(BigDecimal value)
        {
            return Convert.ToUInt32((decimal) value);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static BigDecimal operator +(BigDecimal value)
        {
            return value;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static BigDecimal operator -(BigDecimal value)
        {
            value.Mantissa *= -1;
            return value;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static BigDecimal operator ++(BigDecimal value)
        {
            return value + 1;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static BigDecimal operator --(BigDecimal value)
        {
            return value - 1;
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static BigDecimal operator +(BigDecimal left, BigDecimal right)
        {
            return Add(left, right);
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static BigDecimal operator -(BigDecimal left, BigDecimal right)
        {
            return Add(left, -right);
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private static BigDecimal Add(BigDecimal left, BigDecimal right)
        {
            return left.Exponent > right.Exponent
                       ? new BigDecimal(AlignExponent(left, right) + right.Mantissa, right.Exponent)
                       : new BigDecimal(AlignExponent(right, left) + left.Mantissa, left.Exponent);
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static BigDecimal operator *(BigDecimal left, BigDecimal right)
        {
            return new BigDecimal(left.Mantissa * right.Mantissa, left.Exponent + right.Exponent);
        }

        /// <summary>
        /// </summary>
        /// <param name="dividend"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static BigDecimal operator /(BigDecimal dividend, BigDecimal divisor)
        {
            var exponentChange = Precision - (NumberOfDigits(dividend.Mantissa) - NumberOfDigits(divisor.Mantissa));
            if (exponentChange < 0)
                exponentChange = 0;
            dividend.Mantissa *= BigInteger.Pow(10, exponentChange);
            return new BigDecimal(dividend.Mantissa / divisor.Mantissa,
                dividend.Exponent - divisor.Exponent - exponentChange);
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(BigDecimal left, BigDecimal right)
        {
            return left.Exponent == right.Exponent && left.Mantissa == right.Mantissa;
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(BigDecimal left, BigDecimal right)
        {
            return left.Exponent != right.Exponent || left.Mantissa != right.Mantissa;
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator <(BigDecimal left, BigDecimal right)
        {
            return left.Exponent > right.Exponent
                       ? AlignExponent(left, right) < right.Mantissa
                       : left.Mantissa < AlignExponent(right, left);
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator >(BigDecimal left, BigDecimal right)
        {
            return left.Exponent > right.Exponent
                       ? AlignExponent(left, right) > right.Mantissa
                       : left.Mantissa > AlignExponent(right, left);
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator <=(BigDecimal left, BigDecimal right)
        {
            return left.Exponent > right.Exponent
                       ? AlignExponent(left, right) <= right.Mantissa
                       : left.Mantissa <= AlignExponent(right, left);
        }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator >=(BigDecimal left, BigDecimal right)
        {
            return left.Exponent > right.Exponent
                       ? AlignExponent(left, right) >= right.Mantissa
                       : left.Mantissa >= AlignExponent(right, left);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static BigDecimal Parse(string value)
        {
            //todo culture format
            var decimalCharacter = ".";
            var indexOfDecimal = value.IndexOf(".", StringComparison.Ordinal);
            var exponent = 0;
            if (indexOfDecimal != -1)
                exponent = (value.Length - (indexOfDecimal + 1)) * -1;
            var mantissa = BigInteger.Parse(value.Replace(decimalCharacter, ""));
            return new BigDecimal(mantissa, exponent);
        }

        /// <summary>
        ///     Returns the mantissa of value, aligned to the exponent of reference.
        ///     Assumes the exponent of value is larger than of value.
        /// </summary>
        private static BigInteger AlignExponent(BigDecimal value, BigDecimal reference)
        {
            return value.Mantissa * BigInteger.Pow(10, value.Exponent - reference.Exponent);
        }

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="exponent"></param>
        /// <returns></returns>
        public static BigDecimal Exp(double exponent)
        {
            var tmp = (BigDecimal) 1;
            while (System.Math.Abs(exponent) > 100)
            {
                var diff = exponent > 0 ? 100 : -100;
                tmp *= System.Math.Exp(diff);
                exponent -= diff;
            }

            return tmp * System.Math.Exp(exponent);
        }

        /// <summary>
        ///     @TODO Guart Util
        /// </summary>
        /// <param name="basis"></param>
        /// <param name="exponent"></param>
        /// <returns></returns>
        public static BigDecimal Pow(double basis, double exponent)
        {
            var tmp = (BigDecimal) 1;
            while (System.Math.Abs(exponent) > 100)
            {
                var diff = exponent > 0 ? 100 : -100;
                tmp *= System.Math.Pow(basis, diff);
                exponent -= diff;
            }

            return tmp * System.Math.Pow(basis, exponent);
        }
    }
}