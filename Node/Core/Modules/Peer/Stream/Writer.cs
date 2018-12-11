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
    public class Writer
    {
                /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="data"></param>
        /// <param name="sendLock"></param>
        /// <returns></returns>
        public static async Task<bool> MessageWriteAsync(Peer peer, byte[] data, int messageType, SemaphoreSlim _SendLock=null)
        {
            Console.WriteLine("Write started");
            bool disconnectDetected = false;

            int payloadLength=0;

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
                byte[] headerBytes;
                byte[] messageDiscriptor = MessageDescriptor.BuildDiscriptor(2,22,42);//z
                byte[] message;

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

                headerBytes = header.ToBytesForRLPEncoding();

                int messageLen = headerBytes.Length;
                if (payloadLength > 0)
                {
                    messageLen += payloadLength;
                }

                message = new byte[messageLen];
                
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
                if (_SendLock != null)
                {
                    await _SendLock.WaitAsync();
                    try
                    {
                        peer.SslStream.Write(message, 0, message.Length);
                        peer.SslStream.Flush();
                    }
                    finally
                    {
                        _SendLock.Release();
                    }
                }
                else
                {
                    await peer.SslStream.WriteAsync(message, 0, message.Length);
                    await peer.SslStream.FlushAsync();
                }

                return true;
            }
            catch (ObjectDisposedException ObjDispInner)
            {

                Log.Log.Message("*** MessageWriteAsync server disconnected (obj disposed exception): " + peer.Ip + ":" + peer.Port +
                    ObjDispInner.Message);
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
                    peer.Dispose();
                }
            }
        }

    }
}