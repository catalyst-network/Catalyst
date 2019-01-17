using System;
using System.Linq;
using System.Text;
using Catalyst.Helpers.Hex.HexConverters.Extensions;
using Org.BouncyCastle.Crypto.Digests;
using System.Security.Cryptography;

namespace Catalyst.Helpers.Util
{
    public class Sha3Keccack
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string CalculateRandomHash()
        {
            using(RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[16];
                rng.GetBytes(tokenData);
                return CalculateHash(Convert.ToBase64String(tokenData));
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string CalculateHash(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            var input = Encoding.UTF8.GetBytes(value);
            var output = CalculateHash(input);
            return output.ToHex();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hexValues"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public string CalculateHashFromHex(params string[] hexValues)
        {
            if (hexValues == null) throw new ArgumentNullException(nameof(hexValues));
            if (hexValues.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(hexValues));
            var joinedHex = string.Join("", hexValues.Select(x => x.RemoveHexPrefix()).ToArray());
            return CalculateHash(joinedHex.HexToByteArray()).ToHex();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public byte[] CalculateHash(byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(value));
            var digest = new KeccakDigest(256);
            var output = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(value, 0, value.Length);
            digest.DoFinal(output, 0);
            return output;
        }
    }
}
