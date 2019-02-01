using System.Numerics;

namespace Catalyst.Node.Core.Helpers.Util
{
    public static class BigIntegerExtensions
    {
        public static int NumberOfDigits(this BigInteger value)
        {
            return (value * value.Sign).ToString().Length;
        }
    }
}