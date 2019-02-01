using System;
using Dawn;

namespace Catalyst.Node.Core.Helpers.RLP
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
            Guard.Argument(rlpData, nameof(rlpData)).NotEmpty();
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