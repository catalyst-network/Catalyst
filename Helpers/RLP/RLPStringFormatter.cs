using System;
using System.Text;
using ADL.Hex.HexConverters.Extensions;

namespace ADL.RLP
{
    public class RLPStringFormatter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static string Format(IRlpElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var output = new StringBuilder();
            if (element == null)
                throw new Exception("RLPElement object can't be null");
            if (element is RLPCollection rlpCollection)
            {
                output.Append("[");
                foreach (var innerElement in rlpCollection)
                    Format(innerElement);
                output.Append("]");
            }
            else
            {
                output.Append(element.RlpData.ToHex() + ", ");
            }
            return output.ToString();
        }
    }
}