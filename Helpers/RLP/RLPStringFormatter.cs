using System;
using System.Text;
using ADL.Hex.HexConvertors.Extensions;

namespace ADL.RLP
{
    public class RLPStringFormatter
    {
        public static string Format(IRLPElement element)
        {
            var output = new StringBuilder();
            if (element == null)
                throw new Exception("RLPElement object can't be null");
            var rlpCollection = element as RLPCollection;
            if (rlpCollection != null)
            {
                output.Append("[");
                foreach (var innerElement in rlpCollection)
                    Format(innerElement);
                output.Append("]");
            }
            else
            {
                output.Append(element.RLPData.ToHex() + ", ");
            }
            return output.ToString();
        }
    }
}