using System.Text;

namespace ADL.Hex.HexConvertors.Extensions
{
    public static class HexStringUTF8ConvertorExtensions
    {
        public static string ToHexUTF8(this string value)
        {
            return "0x" + Encoding.UTF8.GetBytes(value).ToHex();
        }


        public static string HexToUTF8String(this string hex)
        {
            var bytes = hex.HexToByteArray();
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);  //@TODO want to hook this into the  ByteToString util method but creates circular dep
        }
    }
}