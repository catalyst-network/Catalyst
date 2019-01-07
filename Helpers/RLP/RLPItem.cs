using System;

namespace ADL.RLP
{
    public class RlpItem : IRlpElement
    {
        private readonly byte[] rlpData;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rlpData"></param>
        /// <exception cref="ArgumentException"></exception>
        public RlpItem(byte[] rlpData)
        {
            if (rlpData.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(rlpData));
            this.rlpData = rlpData;
        }

        public byte[] RlpData => GetRlpData();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private byte[] GetRlpData()
        {
            return rlpData.Length == 0 ? null : rlpData;
        }
    }
}