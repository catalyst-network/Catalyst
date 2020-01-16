#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities;

namespace Lib.P2P.Cryptography
{
    /// <summary>
    ///   The CTR cipher as a stream.
    /// </summary>
    /// <remarks>
    ///   A copy of <see href="https://github.com/onovotny/bc-csharp/blob/netstandard/crypto/src/crypto/modes/SicBlockCipher.cs">SicBlockCipher</see> 
    ///   that implements <see cref="IStreamCipher"/>.
    /// </remarks>
    public class CtrStreamCipher : IStreamCipher
    {
        private readonly IBlockCipher cipher;
        private readonly int blockSize;
        private readonly byte[] counter;
        private readonly byte[] counterOut;
        private int byteCount;
        private byte[] IV;

        /// <summary>
        ///   Creates a new instance of the <see cref="CtrStreamCipher"/> with
        ///   the specified cipher.
        /// </summary>
        /// <param name="cipher">
        ///   The cipher to produce the output counter.  Typically
        ///   <see cref="Org.BouncyCastle.Crypto.Engines.AesEngine"/>.
        /// </param>
        public CtrStreamCipher(IBlockCipher cipher)
        {
            this.cipher = cipher;
            blockSize = cipher.GetBlockSize();
            counter = new byte[blockSize];
            counterOut = new byte[blockSize];
            IV = new byte[blockSize];
        }

        /// <summary>
        ///   The name of this algorithm.
        /// </summary>
        public string AlgorithmName => cipher.AlgorithmName + "/CTR";

        /// <summary>
        ///   Init the cipher.
        /// </summary>
        /// <param name="forEncryption">
        ///   Ignored.
        /// </param>
        /// <param name="parameters">
        ///   Must be a <see cref="ParametersWithIV"/>.
        /// </param>
        /// <example>
        /// var encrypt = new CtrStreamCipher(new AesEngine());
        /// var p = new ParametersWithIV(new KeyParameter(key), iv);
        /// encrypt.Init(true, p);
        /// </example>
        public void Init(bool forEncryption, ICipherParameters parameters)
        {
            var ivParam = parameters as ParametersWithIV;
            if (ivParam == null)
                throw new ArgumentException("CTR mode requires ParametersWithIV", "parameters");

            IV = Arrays.Clone(ivParam.GetIV());

            if (blockSize < IV.Length)
                throw new ArgumentException("CTR mode requires IV no greater than: " + blockSize + " bytes.");

            var maxCounterSize = Math.Min(8, blockSize / 2);
            if (blockSize - IV.Length > maxCounterSize)
                throw new ArgumentException("CTR mode requires IV of at least: " + (blockSize - maxCounterSize) +
                    " bytes.");

            // if null it's an IV changed only.
            if (ivParam.Parameters != null) cipher.Init(true, ivParam.Parameters);

            Reset();
        }

        /// <inheritdoc />
        public void Reset()
        {
            byteCount = 0;
            Arrays.Fill(counter, (byte) 0);
            Array.Copy(IV, 0, counter, 0, IV.Length);
            cipher.Reset();
        }

        /// <inheritdoc />
        public void ProcessBytes(byte[] input, int inOff, int length, byte[] output, int outOff)
        {
            if (outOff + length > output.Length) throw new DataLengthException("Output buffer too short");

            if (inOff + length > input.Length) throw new DataLengthException("Input buffer too small");

            var inStart = inOff;
            var inEnd = inOff + length;
            var outStart = outOff;

            while (inStart < inEnd) output[outStart++] = ReturnByte(input[inStart++]);
        }

        /// <inheritdoc />
        public byte ReturnByte(byte input)
        {
            if (byteCount == 0)
            {
                cipher.ProcessBlock(counter, 0, counterOut, 0);

                // Increment the counter
                var j = counter.Length;
                while (--j >= 0 && ++counter[j] == 0) { }
            }

            var rv = (byte) (counterOut[byteCount++] ^ input);
            if (byteCount == counterOut.Length) byteCount = 0;
            return rv;
        }
    }
}
