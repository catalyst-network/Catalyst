using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using Catalyst.Node.Core.Helpers.Logger;
using Catalyst.Node.Core.Helpers.Util;
using Nethereum.RLP;

namespace Catalyst.Node.Core.Helpers.Streams
{
    public static class Writer
    {
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
                    Log.Message("MessageWriteAsync SSL stream is null");
                    disconnectDetected = true;
                    return false;
                }

                var header = "";

                foreach (int i in messageDescriptor) Log.Message(i.ToString());

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
                if (payloadLength > 0) messageLen += payloadLength;

                var message = new byte[messageLen]; //@TODO hook into new byte mthod

                data = ByteUtil.CombineByteArr(messageDescriptor, data);

                Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);

                if (data != null && data.Length > 0)
                    Buffer.BlockCopy(data, 0, message, headerBytes.Length, data.Length);

                sslStream.Write(message, 0, message.Length);
                sslStream.Flush();
                return true;
            }
            catch (ObjectDisposedException objDipInner)
            {
                disconnectDetected = true;
                LogException.Message("*** MessageWriteAsync server disconnected (obj disposed exception)", objDipInner);
                return false;
            }
            catch (SocketException sockInner)
            {
                disconnectDetected = true;
                LogException.Message("*** MessageWriteAsync server disconnected (socket exception): ", sockInner);
                return false;
            }
            catch (InvalidOperationException invOpInner)
            {
                LogException.Message("*** MessageWriteAsync server disconnected (invalid operation exception): ",
                    invOpInner);
                disconnectDetected = true;
                return false;
            }
            catch (IOException ioInner)
            {
                LogException.Message("*** MessageWriteAsync server disconnected (IO exception): ", ioInner);
                disconnectDetected = true;
                return false;
            }
            catch (Exception e)
            {
                LogException.Message("MessageWriteAsync", e);
                disconnectDetected = true;
                return false;
            }
            finally
            {
                if (disconnectDetected) sslStream?.Dispose();
            }
        }
    }
}