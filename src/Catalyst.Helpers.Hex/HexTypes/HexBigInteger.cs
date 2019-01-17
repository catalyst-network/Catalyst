using System;
using System.Numerics;
using Catalyst.Helpers.Hex.HexConverters;
using Newtonsoft.Json;

namespace Catalyst.Helpers.Hex.HexTypes
{
    [JsonConverter(typeof(HexRpcTypeJsonConverter<HexBigInteger, BigInteger>))]
    public class HexBigInteger : HexRpcType<BigInteger>
    {
        public HexBigInteger(string hex) : base(new HexBigIntegerBigEndianConverter(), hex)
        {
        }

        public HexBigInteger(BigInteger value) : base(value, new HexBigIntegerBigEndianConverter())
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (obj is HexBigInteger val)
            {
                return val.Value == Value;
            }

            return false;
        }
    }
}