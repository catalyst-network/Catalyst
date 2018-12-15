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
        /// <param name="connectionMeta"></param>
        /// <param name="data"></param>
        /// <param name="sendLock"></param>
        /// <returns></returns>
        public static async Task<bool> MessageWriteAsync(ConnectionMeta connectionMeta, byte[] data, int messageType, SemaphoreSlim sendLock=null)
        {
            int payloadLength=0;
            bool disconnectDetected = false;
            
            try
            {
                if (connectionMeta == null)
                {
                    Log.Log.Message("MessageWriteAsync peer is null");
                    disconnectDetected = true;
                    return false;
                }
                if (connectionMeta.SslStream == null)
                {
                    Log.Log.Message("MessageWriteAsync SSL stream is null");
                    disconnectDetected = true;
                    return false;
                }
                
                string header = "";
                byte[] messageDiscriptor = MessageDescriptor.BuildDiscriptor(2,22,42);//z

                foreach (int i in messageDiscriptor)
                {
                    Console.WriteLine(i);
                }

                if (data == null || data.Length < 1)
                {
                    header += "0:";
                    header += messageDiscriptor.Length+":";
                }
                else
                {
                    payloadLength = messageDiscriptor.Length + data.Length;
                    header += payloadLength+":";
                }

                var headerBytes = header.ToBytesForRLPEncoding();

                int messageLen = headerBytes.Length;
                if (payloadLength > 0)
                {
                    messageLen += payloadLength;
                }

                var message = new byte[messageLen];
                
                Console.WriteLine(BitConverter.ToString(messageDiscriptor));
                Console.WriteLine(BitConverter.ToString(data));

                data = ByteUtil.CombineByteArr(messageDiscriptor,data);
                Console.WriteLine(BitConverter.ToString(data));
          
                Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);

                if (data != null && data.Length > 0)
                {
                    Buffer.BlockCopy(data, 0, message, headerBytes.Length, data.Length);
                }
                
                // use semaphore to lock thread while we write to peer
                if (sendLock != null)
                {
                    await sendLock.WaitAsync();
                    try
                    {
                        connectionMeta.SslStream.Write(message, 0, message.Length);
                        connectionMeta.SslStream.Flush();
                    }
                    finally
                    {
                        sendLock.Release();
                    }
                }
                else
                {
                    await connectionMeta.SslStream.WriteAsync(message, 0, message.Length);
                    await connectionMeta.SslStream.FlushAsync();
                }
                return true;
            }
            catch (ObjectDisposedException objDispInner)
            {
                Log.Log.Message("*** MessageWriteAsync server disconnected (obj disposed exception): " + connectionMeta.Ip + ":" + connectionMeta.Port +
                    objDispInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (SocketException SockInner)
            {
                Log.Log.Message("*** MessageWriteAsync server disconnected (socket exception): " + SockInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (InvalidOperationException InvOpInner)
            {
                Log.Log.Message("*** MessageWriteAsync server disconnected (invalid operation exception): " + InvOpInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (IOException IOInner)
            {
                Log.Log.Message("*** MessageWriteAsync server disconnected (IO exception): " + IOInner.Message);
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
                    connectionMeta?.Dispose();
                }
            }
        }

    }
}