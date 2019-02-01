using System.Collections.Generic;

namespace Catalyst.Node.Core.Helpers.RLP
{
    public class RlpCollection : List<IRlpElement>, IRlpElement
    {
        /// <summary>
        /// </summary>
        public byte[] RlpData { get; set; }
    }
}