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
using System.Net.Security;
using System.Reflection;
using Catalyst.Node.Common.Helpers.Util;
using Nethereum.RLP;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Streams
{
    public static class Writer
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// </summary>
        /// <param name="sslStream"></param>
        /// <param name="data"></param>
        /// <param name="messageType"></param>
        /// <param name="messageDescriptor"></param>
        /// <returns></returns>
        public static bool MessageWrite(SslStream sslStream, byte[] data, int messageType, byte[] messageDescriptor)
        {
            var payloadLength = 0;
            var disconnectDetected = false;

            try
            {
                if (sslStream == null)
                {
                    Logger.Warning("MessageWriteAsync SSL stream is null");
                    disconnectDetected = true;
                    return false;
                }

                var header = "";

                foreach (int i in messageDescriptor) Logger.Information(i.ToString());

                if (data == null || data.Length < 1)
                {
                    header += "0:";
                    header += messageDescriptor.Length + ":";
                }
                else
                {
                    payloadLength = messageDescriptor.Length + data.Length;
                    header += payloadLength + ":";
                }

                var headerBytes = header.ToBytesForRLPEncoding();

                var messageLen = headerBytes.Length;
                if (payloadLength > 0)
                {
                    messageLen += payloadLength;
                }

                var message = new byte[messageLen]; //@TODO hook into new byte mthod

                var dataWithDescriptor = ByteUtil.CombineByteArrays(messageDescriptor, data);

                Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);

                if (dataWithDescriptor != null && dataWithDescriptor.Length > 0) {
                    Buffer.BlockCopy(dataWithDescriptor, 0, message, headerBytes.Length,
                        dataWithDescriptor.Length);
                }

                sslStream.Write(message, 0, message.Length);
                sslStream.Flush();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "MessageWriteAsync");
                disconnectDetected = true;
                return false;
            }
            finally
            {
                if (disconnectDetected)
                {
                    sslStream?.Dispose();
                }
            }
        }
    }
}