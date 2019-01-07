using System.Collections.Generic;

namespace ADL.RLP
{
    public class RLPCollection : List<IRlpElement>, IRlpElement
    {
        public byte[] RlpData { get; set; }
    }
}