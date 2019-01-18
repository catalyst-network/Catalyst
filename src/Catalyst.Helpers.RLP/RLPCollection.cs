using System.Collections.Generic;

namespace Catalyst.Helpers.RLP
{
    public class RlpCollection : List<IRlpElement>, IRlpElement
    {
        /// <summary>
        /// </summary>
        public byte[] RlpData { get; set; }
    }
}