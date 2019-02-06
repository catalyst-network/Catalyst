using System;
using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RLP;

namespace Catalyst.Node.Core.Helpers.Util
{
    public static class ContractUtils
    {
        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="nonce"></param>
        /// <returns></returns>
        public static string CalculateContractAddress(string address, BigInteger nonce)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            var sha3 = new Sha3Keccack();
            return
                sha3.CalculateHash(RLP.EncodeList(RLP.EncodeElement(address.HexToByteArray()),
                    RLP.EncodeElement(nonce.ToBytesForRLPEncoding()))).ToHex().Substring(24);
        }
    }
}