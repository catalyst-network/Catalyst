using System;
using System.Numerics;

namespace ADL.Hex.HexTypes
{
    public class HexTypeFactory
    {
        public static object CreateFromHex<T>(string hex)
        {
            if (typeof(BigInteger) == typeof(T))
                return new HexBigInteger(hex);

            if (typeof(string) == typeof(T))
                return HexUTF8String.CreateFromHex(hex);
            throw new ArgumentOutOfRangeException();
        }
    }
}