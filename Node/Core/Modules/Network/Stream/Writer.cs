using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Modules.Network.Messages;
using ADL.RLP;
using ADL.Util;
using ADL.Node.Core.Modules.Network.Connections;

namespace ADL.Node.Core.Modules.Network.Stream
{
    public static class Writer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="data"></param>
        /// <param name="messageType"></param>
        /// <param name="sendLock"></param>
        /// <returns></returns>
        public static bool MessageWrite(Connection connection, byte[] data, int messageType)
        {
            int payloadLength=0;
            bool disconnectDetected = false;
            
            try
            {
                if (connection == null)
                {
                    Log.Log.Message("MessageWriteAsync peer is null");
                    disconnectDetected = true;
                    return false;
                }
                if (connection.SslStream == null)
                {
                    Log.Log.Message("MessageWriteAsync SSL stream is null");
                    disconnectDetected = true;
                    return false;
                }
                
                string header = "";
                byte[] messageDescriptor = Message.BuildMsgDescriptor(2,42);

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

                var headerBytes = header.ToBytesForRlpEncoding();

                int messageLen = headerBytes.Length;
                if (payloadLength > 0)
                {
                    messageLen += payloadLength;
                }

                var message = new byte[messageLen];//@TODO hook into new byte mthod

                data = ByteUtil.CombineByteArr(messageDescriptor,data);
          
                Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);

                if (data != null && data.Length > 0)
                {
                    Buffer.BlockCopy(data, 0, message, headerBytes.Length, data.Length);
                }
                
                connection.SslStream.Write(message, 0, message.Length);
                connection.SslStream.Flush();
                return true;
            }
            catch (ObjectDisposedException objDipInner)
            {
                disconnectDetected = true;
                if (connection != null)
                    Log.LogException.Message("*** MessageWriteAsync server disconnected (obj disposed exception): " + connection.EndPoint.Address + ":" + connection.EndPoint.Port, objDipInner);
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
                    connection?.Dispose();
                }
            }
        }
    }
}
