using System;
using System.Net.Security;
using Catalyst.Node.Core.Helpers.Util;
using Nethereum.RLP;
using Serilog;

namespace Catalyst.Node.Core.Helpers.Streams
{
    public static class Writer
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

                foreach (int i in messageDescriptor)
                {
                    Logger.Information(i.ToString());
                }

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

                data = ByteUtil.CombineByteArr(messageDescriptor, data);

                Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);

                if (data != null && data.Length > 0)
                {
                    Buffer.BlockCopy(data, 0, message, headerBytes.Length, data.Length);
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