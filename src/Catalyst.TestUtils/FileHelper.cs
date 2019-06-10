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
using System.IO;
using ICSharpCode.SharpZipLib.Checksum;

namespace Catalyst.TestUtils
{
    public static class FileHelper
    {
        /// <summary>Gets the CRC value.</summary>
        /// <param name="crcBytes">The CRC bytes.</param>
        /// <returns></returns>
        public static long GetCrcValue(this byte[] crcBytes)
        {
            Crc32 crc32 = new Crc32();
            crc32.Update(crcBytes);
            return crc32.Value;
        }

        /// <summary>Gets the CRC value.</summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static long GetCrcValue(Stream stream)
        {
            byte[] streamBytes = new byte[stream.Length];
            var read = stream.Read(streamBytes, 0, streamBytes.Length);

            if (read != streamBytes.Length)
            {
                return -1;
            }

            return streamBytes.GetCrcValue();
        }

        /// <summary>Gets the CRC value.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        public static long GetCrcValue(string filePath)
        {
            Crc32 crc32 = new Crc32();
            crc32.Update(File.ReadAllBytes(filePath));
            return crc32.Value;
        }

        /// <summary>Creates the temporary file.</summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public static string CreateTempFile(byte[] bytes)
        {
            string filePath = Path.GetTempFileName();

            if (bytes != null)
            {
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }

            return filePath;
        }

        /// <summary>Creates the temporary file.</summary>
        /// <returns></returns>
        public static string CreateTempFile() { return CreateTempFile(null); }

        /// <summary>Gets the random bytes.</summary>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public static byte[] GetRandomBytes(long size)
        {
            var fileBytes = new byte[size];
            new Random().NextBytes(fileBytes);
            return fileBytes;
        }

        /// <summary>Creates the random temporary file.</summary>
        /// <param name="byteSize">Size of the file.</param>
        /// <returns></returns>
        public static string CreateRandomTempFile(long byteSize) { return CreateTempFile(GetRandomBytes(byteSize)); }
    }
}
