using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Helpers.Logger;
using Catalyst.Node.Modules.Core.P2P.Messages;
using Catalyst.Helpers.RLP;
using Catalyst.Helpers.Util;
using Catalyst.Node.Modules.Core.P2P.Connections;

namespace Catalyst.Node.Modules.Core.P2P.Stream
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
            int payloCatalystength=0;
            bool disconnectDetected = false;
            
            try
            {
                if (connection == null)
                {
                    Log.Message("MessageWriteAsync peer is null");
                    disconnectDetected = true;
                    return false;
                }
                if (connection.SslStream == null)
                {
                    Log.Message("MessageWriteAsync SSL stream is null");
                    disconnectDetected = true;
                    return false;
                }
                
                string header = "";
                byte[] messageDescriptor = Message.BuildMsgDescriptor(2,42);

                foreach (int i in messageDescriptor)
                {
                    Log.Message(i.ToString());
                }

                if (data == null || data.Length < 1)
                {
                    header += "0:";
                    header += messageDescriptor.Length+":";
                }
                else
                {
                    payloCatalystength = messageDescriptor.Length + data.Length;
                    header += payloCatalystength+":";
                }

                var headerBytes = header.ToBytesForRlpEncoding();

                int messageLen = headerBytes.Length;
                if (payloCatalystength > 0)
                {
                    messageLen += payloCatalystength;
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
                    LogException.Message("*** MessageWriteAsync server disconnected (obj disposed exception): " + connection.EndPoint.Address + ":" + connection.EndPoint.Port, objDipInner);
                return false;
            }
            catch (SocketException sockInner)
            {
                disconnectDetected = true;
                LogException.Message("*** MessageWriteAsync server disconnected (socket exception): " , sockInner);
                return false;
            }
            catch (InvalidOperationException invOpInner)
            {
                LogException.Message("*** MessageWriteAsync server disconnected (invalid operation exception): ", invOpInner);
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
                if (disconnectDetected)
                {
                    connection?.Dispose();
                }
            }
        }
    }
}
