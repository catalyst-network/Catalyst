using System;
using System.Numerics;
using Catalyst.Helpers.Hex.HexConverters.Extensions;

namespace Catalyst.Helpers.Hex.HexConverters
{
    public class HexBigIntegerBigEndianConverter : IHexConvertor<BigInteger>
    {
        /// <summary>
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public string ConvertToHex(BigInteger newValue)
        {
            return newValue.ToHex(false);
        }

        /// <summary>
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public BigInteger ConvertFromHex(string hex)
        {
            if (hex == null) throw new ArgumentNullException(nameof(hex));
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(hex));
            return hex.HexToBigInteger(false);
        }
    }
}