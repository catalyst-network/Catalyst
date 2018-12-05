using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;

namespace ADL.Node.Core.Modules.Peer.IO
{
    
    /// <summary>
    /// 
    /// </summary>
    public abstract class ServerBase
    {
        internal bool _debug = false;
        internal bool Disposed = false;
        internal bool AcceptInvalidCerts;
        internal CancellationTokenSource _TokenSource;
        internal CancellationToken _Token;
        internal Func<string, int, bool> _PeerConnected = null;
        internal Func<string, int, bool> _PeerDisconnected = null;
        internal Func<string, int, byte[], bool> _MessageReceived = null;
        internal ConcurrentDictionary<string, Peer> _Clients;
        internal bool _Connected;

        /// <summary>
        /// 
        /// </summary>
        public abstract void Dispose();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        internal async Task<byte[]> MessageReadAsync(Peer client)
        {
            int bytesRead = 0;
            int sleepInterval = 25;
            int maxTimeout = 500;
            int currentTimeout = 0;
            bool timeout = false;

            byte[] headerBytes;
            string header = "";
            long contentLength;
            byte[] contentBytes;

            if (!client.SslStream.CanRead)
            {
                return null;
            }

            using (MemoryStream headerMs = new MemoryStream())
            {
                byte[] headerBuffer = new byte[1];
                timeout = false;
                currentTimeout = 0;
                int read = 0;

                while ((read = await client.SslStream.ReadAsync(headerBuffer, 0, headerBuffer.Length)) > 0)
                {
                    if (read > 0)
                    {
                        await headerMs.WriteAsync(headerBuffer, 0, read);
                        bytesRead += read;

                        // reset timeout since there was a successful read
                        currentTimeout = 0;
                    }
                    else
                    {
                        if (currentTimeout >= maxTimeout)
                        {
                            timeout = true;
                            break;
                        }
                        else
                        {
                            currentTimeout += sleepInterval;
                            await Task.Delay(sleepInterval);
                        }

                        if (timeout)
                        {
                            break;
                        }
                    }

                    if (bytesRead > 1)
                    {
                        // check if end of headers reached
                        if (headerBuffer[0] == 58)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (currentTimeout >= maxTimeout)
                        {
                            timeout = true;
                            break;
                        }
                        else
                        {
                            currentTimeout += sleepInterval;
                            await Task.Delay(sleepInterval);
                        }

                        if (timeout)
                        {
                            break;
                        }
                    }
                }

                if (timeout)
                {
                    Log("*** MessageReadAsync timeout " + currentTimeout + "ms/" + maxTimeout + "ms exceeded while reading header after reading " + bytesRead + " bytes");
                    return null;
                }

                headerBytes = headerMs.ToArray();
                if (headerBytes == null || headerBytes.Length < 1)
                {
                    return null;
                }

                header = Encoding.UTF8.GetString(headerBytes);
                header = header.Replace(":", "");

                if (!Int64.TryParse(header, out contentLength))
                {
                    Log("*** MessageReadAsync malformed message from " + client.Ip + client.Port + " (message header not an integer)");
                    return null;
                }
            }
            
            using (MemoryStream dataMs = new MemoryStream())
            {
                long bytesRemaining = contentLength;
                timeout = false;
                currentTimeout = 0;

                int read = 0;
                byte[] buffer;
                long bufferSize = 2048;
                if (bufferSize > bytesRemaining)
                {
                    bufferSize = bytesRemaining;
                }

                buffer = new byte[bufferSize];

                while ((read = await client.SslStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    if (read > 0)
                    {
                        dataMs.Write(buffer, 0, read);
                        bytesRead = bytesRead + read;
                        bytesRemaining = bytesRemaining - read;

                        // reset timeout
                        currentTimeout = 0;

                        // reduce buffer size if number of bytes remaining is
                        // less than the pre-defined buffer size of 2KB
                        if (bytesRemaining < bufferSize)
                        {
                            bufferSize = bytesRemaining;
                        }

                        buffer = new byte[bufferSize];

                        // check if read fully
                        if (bytesRemaining == 0)
                        {
                            break;
                        }

                        if (bytesRead == contentLength)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (currentTimeout >= maxTimeout)
                        {
                            timeout = true;
                            break;
                        }
                        else
                        {
                            currentTimeout += sleepInterval;
                            await Task.Delay(sleepInterval);
                        }

                        if (timeout)
                        {
                            break;
                        }
                    }
                }

                if (timeout)
                {
                    Log("*** MessageReadAsync timeout " + currentTimeout + "ms/" + maxTimeout + "ms exceeded while reading content after reading " + bytesRead + " bytes");
                    return null;
                }

                contentBytes = dataMs.ToArray();
            }

            if (contentBytes == null || contentBytes.Length < 1)
            {
                Log("*** MessageReadAsync " + client.Ip + client.Port + " no content read");
                return null;
            }

            if (contentBytes.Length != contentLength)
            {
                Log("*** MessageReadAsync " + client.Ip + client.Port + " content length " + contentBytes.Length + " bytes does not match header value " + contentLength + ", discarding");
                return null;
            }
            return contentBytes;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(string ip, int port, byte[] data)
        {
            if (!_Clients.TryGetValue(ip+":"+port, out Peer client))
            {
                Log("*** SendAsync unable to find client " + ip+":"+port);
                return false;
            }

            return await MessageWriteAsync(client, data);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="sendLock"></param>
        /// <returns></returns>
        private async Task<bool> MessageWriteAsync(Peer client, byte[] data, SemaphoreSlim sendLock=null)
        {
            bool disconnectDetected = false;

            try
            {
                if (client == null)
                {
                    Log("MessageWriteAsync client is null");
                    disconnectDetected = true;
                    return false;
                }

                if (client.SslStream == null)
                {
                    Log("MessageWriteAsync SSL stream is null");
                    disconnectDetected = true;
                    return false;
                }

                string header = "";
                byte[] headerBytes;
                byte[] message;

                if (data == null || data.Length < 1)
                {
                    header += "0:";
                }
                else
                {
                    header += data.Length + ":";
                }

                headerBytes = Encoding.UTF8.GetBytes(header);
                int messageLen = headerBytes.Length;
                if (data != null && data.Length > 0)
                {
                    messageLen += data.Length;
                }

                message = new byte[messageLen];
                Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);

                if (data != null && data.Length > 0)
                {
                    Buffer.BlockCopy(data, 0, message, headerBytes.Length, data.Length);
                }

                if (sendLock != null)
                {
                    await sendLock.WaitAsync();
                    try
                    {
                        client.SslStream.Write(message, 0, message.Length);
                        client.SslStream.Flush();
                    }
                    finally
                    {
                        sendLock.Release();
                    }
                }
                else
                {
                    await client.SslStream.WriteAsync(message, 0, message.Length);
                    await client.SslStream.FlushAsync();                    
                }
                return true;
            }
            catch (ObjectDisposedException ObjDispInner)
            {

                Log("*** MessageWriteAsync server disconnected (obj disposed exception): " + client.Ip+":"+client.Port + ObjDispInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (SocketException SockInner)
            {
                Log("*** MessageWriteAsync server disconnected (socket exception): " + SockInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (InvalidOperationException InvOpInner)
            {
                Log("*** MessageWriteAsync server disconnected (invalid operation exception): " + InvOpInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (IOException IOInner)
            {
                Log("*** MessageWriteAsync server disconnected (IO exception): " + IOInner.Message);
                disconnectDetected = true;
                return false;
            }
            catch (Exception e)
            {
                LogException("MessageWriteAsync", e);
                disconnectDetected = true;
                return false;
            }
            finally
            {
                if (disconnectDetected)
                {
                    _Connected = false;
                    Dispose();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        internal static bool PeerConnected(string ip, int port)
        {
            Console.WriteLine("Client connected: "+ip+":"+port);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        internal static bool PeerDisconnected(string ip, int port)
        {
            Console.WriteLine("Client disconnected: " + ip+":"+port);
            return true;
        }
        
        /// <summary>
        /// @TODO here we need to do a message router.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static bool MessageReceived(string ip, int port, byte[] data)
        {
            Console.WriteLine("lgdflal");

            string msg = "";
            if (data != null && data.Length > 0)
            {
                msg = Encoding.UTF8.GetString(data);
            }
            Console.WriteLine("dfds");

            var charResponse = ADL.Protocol.Peer.ChallengeResponse.Parser.ParseFrom(data);
            Console.WriteLine("dfds");

            var keyFactory = PrivateKeyFactory.CreateKey(System.Convert.FromBase64String(charResponse.PublicKey));
            Console.WriteLine("llal");
//            Console.WriteLine(Ec.VerifySignature(keyFactory,charResponse.SignedNonce,client.nonce.ToString()));

            Console.WriteLine("Message received from " + ip+":"+port + ": " + ADL.Protocol.Peer.ChallengeResponse.Parser.ParseFrom(data));
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        internal bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // return true; // Allow untrusted certificates.
            return AcceptInvalidCerts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        internal void Log(string msg)
        {
            if (_debug)
            {
                Console.WriteLine(msg);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="e"></param>
        internal void LogException(string method, Exception e)
        {
            Log("================================================================================");
            Log(" = Method: " + method);
            Log(" = Exception Type: " + e.GetType().ToString());
            Log(" = Exception Data: " + e.Data);
            Log(" = Inner Exception: " + e.InnerException);
            Log(" = Exception Message: " + e.Message);
            Log(" = Exception Source: " + e.Source);
            Log(" = Exception StackTrace: " + e.StackTrace);
            Log("================================================================================");
        }
    }
}
