using System;
using ADL.Hex.HexConverters;
using Newtonsoft.Json;

namespace ADL.Hex.HexTypes
{
    [JsonConverter(typeof(HexRpcTypeJsonConverter<HexUTF8String, string>))]
    public class HexUTF8String : HexRpcType<string>
    {
        private HexUTF8String() : base(new HexUtf8StringConverter())
        {
        }

        public HexUTF8String(string value) : base(value, new HexUtf8StringConverter())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static HexUTF8String CreateFromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("Value cannot be null or empty.", nameof(hex));
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(hex));
            return new HexUTF8String {HexValue = hex};
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public override bool Equals(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (obj is HexUTF8String val)
            {
                return val.Value == Value;
            }
            return false;
        }
    }
}
