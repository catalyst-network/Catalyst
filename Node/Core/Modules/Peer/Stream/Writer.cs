using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Modules.Peer.Messages;
using ADL.RLP;
using ADL.Util;

namespace ADL.Node.Core.Modules.Peer.Stream
{
    public static class Writer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="data"></param>
        /// <param name="messageType"></param>
        /// <param name="sendLock"></param>
        /// <returns></returns>
        public static bool MessageWrite(Peer peer, byte[] data, int messageType)
        {
            int payloadLength=0;
            bool disconnectDetected = false;
            
            try
            {
                if (peer == null)
                {
                    Log.Log.Message("MessageWriteAsync peer is null");
                    disconnectDetected = true;
                    return false;
                }
                if (peer.SslStream == null)
                {
                    Log.Log.Message("MessageWriteAsync SSL stream is null");
                    disconnectDetected = true;
                    return false;
                }
                
                string header = "";
                byte[] messageDescriptor = MessageDescriptor.BuildDiscriptor(2,22,42);

                foreach (int i in messageDescriptor)
                {
                    Console.WriteLine(i);
                }

                if (data == null || data.Length < 1)
                {
                    header += "0:";
                    header += messageDescriptor.Length+":";
                }
                else
                {
                    payloadLength = messageDescriptor.Length + data.Length;
                    header += payloadLength+":";
                }

                var headerBytes = header.ToBytesForRLPEncoding();

                int messageLen = headerBytes.Length;
                if (payloadLength > 0)
                {
                    messageLen += payloadLength;
                }

                var message = new byte[messageLen];
                
                Console.WriteLine(BitConverter.ToString(messageDescriptor));
                Console.WriteLine(BitConverter.ToString(data));

                data = ByteUtil.CombineByteArr(messageDescriptor,data);
                Console.WriteLine(BitConverter.ToString(data));
          
                Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);

                if (data != null && data.Length > 0)
                {
                    Buffer.BlockCopy(data, 0, message, headerBytes.Length, data.Length);
                }
                
                peer.SslStream.Write(message, 0, message.Length);
                peer.SslStream.Flush();
                return true;
            }
            catch (ObjectDisposedException objDipInner)
            {
                disconnectDetected = true;
                if (peer != null)
                    Log.LogException.Message("*** MessageWriteAsync server disconnected (obj disposed exception): " + peer.Ip + ":" + peer.Port, objDipInner);
                return false;
            }
            catch (SocketException sockInner)
            {
                disconnectDetected = true;
                Log.LogException.Message("*** MessageWriteAsync server disconnected (socket exception): " , sockInner);
                return false;
            }
            catch (InvalidOperationException invOpInner)
            {
                Log.LogException.Message("*** MessageWriteAsync server disconnected (invalid operation exception): ", invOpInner);
                disconnectDetected = true;
                return false;
            }
            catch (IOException ioInner)
            {
                Log.LogException.Message("*** MessageWriteAsync server disconnected (IO exception): ", ioInner);
                disconnectDetected = true;
                return false;
            }
            catch (Exception e)
            {
                Log.LogException.Message("MessageWriteAsync", e);
                disconnectDetected = true;
                return false;
            }
            finally
            {
                if (disconnectDetected)
                {
                    peer?.Dispose();
                }
            }
        }

    }
}
