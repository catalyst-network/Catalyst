#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MultiFormats;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace Lib.P2P.Cryptography
{
    /// <summary>
    ///   A secret (symmetric) key shared among some entities.
    /// </summary>
    /// <remarks>
    ///   This is specifically used for nodes in a private network.
    /// </remarks>
    /// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
    public class PreSharedKey
    {
        private const string CodecName = "/key/swarm/psk/1.0.0/";

        /// <summary>
        ///   The key value.
        /// </summary>
        /// <value>
        ///   The value as a byte array.
        /// </value>
        public byte[] Value { get; set; }

        /// <summary>
        ///   The length of the key's value.
        /// </summary>
        /// <value>
        ///   The length in bits.
        /// </value>
        public int Length => Value?.Length * 8 ?? 0;

        /// <summary>
        ///   Gets an ID for the key.
        /// </summary>
        /// <returns>
        ///   A byte array that can be used as an identifier for the key.
        /// </returns>
        /// <remarks>
        ///   C# implementation of the GO code at 
        ///   <see href="https://github.com/libp2p/go-libp2p-pnet/blob/bed5e6afdf9099121029f6fb675be12a50196114/fingerprint.go#L10"/>.
        /// </remarks>
        public byte[] Fingerprint()
        {
            // Encrypt data first so we don't feed PSK to hash function.
            // Salsa20 function is not reversible thus increasing our security margin.
            var encrypted = new byte[64];
            var nonce = Encoding.ASCII.GetBytes("finprint");
            var cipher = new Salsa20Engine();
            cipher.Init(true, new ParametersWithIV(new KeyParameter(Value), nonce));
            cipher.ProcessBytes(encrypted, 0, encrypted.Length, encrypted, 0);

            // Then do Shake-128 hash to reduce its length.
            // This way if for some reason Shake is broken and Salsa20 preimage is possible,
            // attacker has only half of the bytes necessary to recreate psk.
            return MultiHash
               .GetHashAlgorithm("shake-128")
               .ComputeHash(encrypted);
        }

        /// <summary>
        ///   Generate a new value of the specified length.
        /// </summary>
        /// <param name="length">
        ///   The length in bits of the new key value, defaults to 256.
        /// </param>
        /// <returns>
        ///   <b>this</b> for a fluent design.
        /// </returns>
        public PreSharedKey Generate(int length = 256)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                Value = new byte[length / 8];
                rng.GetBytes(Value);
            }

            return this;
        }

        /// <summary>
        ///   Write the key to text stream.
        /// </summary>
        /// <param name="text">
        ///   A text writer.
        /// </param>
        /// <param name="format">
        ///   Determines how the key <see cref="Value"/> is formatted.  Can be
        ///   "base16" or "base64".  Defaults to "base16".
        /// </param>
        /// <remarks>
        ///   The key is writen as three lines 
        ///   (1) the codec name "/key/swarm/psk/1.0.0/"
        ///   (2) the base encoding  "/base16/" or "/base64/", 
        ///   (3) the key value in the base encoding
        /// </remarks>
        public void Export(TextWriter text, string format = "base16")
        {
            text.WriteLine(CodecName);
            text.Write("/");
            text.Write(format);
            text.WriteLine("/");
            switch (format)
            {
                case "base16":
                    text.WriteLine(Value.ToHexString());
                    break;
                case "base64":
                    text.WriteLine(Base64NoPad.ToBase64NoPad(Value));
                    break;
                default:
                    throw new Exception($"Unknown encoding '{format}'.");
            }
        }

        /// <summary>
        ///   Read the key from the text stream.
        /// </summary>
        /// <param name="text">
        ///   A text reader.
        /// </param>
        public void Import(TextReader text)
        {
            if (text.ReadLine() != CodecName)
                throw new FormatException($"Expected '{CodecName}'.");
            switch (text.ReadLine())
            {
                case "/base16/":
                    Value = text.ReadLine().ToHexBuffer();
                    break;
                case "/base64/":
                    Value = Base64NoPad.FromBase64NoPad(text.ReadLine());
                    break;
                default:
                    throw new FormatException("Unknown base encoding.");
            }
        }
    }
}
