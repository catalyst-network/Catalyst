using System;
using System.Numerics;

namespace Catalyst.Helpers.Hex.HexTypes
{
    public static class HexTypeFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hex"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static object CreateFromHex<T>(string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("Value cannot be null or empty.", nameof(hex));
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(hex));
            if (typeof(BigInteger) == typeof(T))
                return new HexBigInteger(hex);

            if (typeof(string) == typeof(T))
                return HexUTF8String.CreateFromHex(hex);
            throw new ArgumentOutOfRangeException();
        }
    }
}