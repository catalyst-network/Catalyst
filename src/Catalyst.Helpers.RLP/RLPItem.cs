using System;

namespace Catalyst.Helpers.RLP
{
    public class RlpItem : IRlpElement
    {
        private readonly byte[] _data;

        /// <summary>
        /// </summary>
        /// <param name="rlpData"></param>
        /// <exception cref="ArgumentException"></exception>
        public RlpItem(byte[] rlpData)
        {
            //@TODO guard util
            if (rlpData.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(rlpData));
            _data = rlpData;
        }

        public byte[] RlpData => GetRlpData();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private byte[] GetRlpData()
        {
            return _data.Length == 0 ? null : RlpData;
        }
    }
}