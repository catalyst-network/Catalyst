using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using ADL.Util;

namespace ADL.Node.Core.Modules.Peer.Stream
{
    public class Reader
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public static async Task<byte[]> MessageReadAsync(Peer peer)
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

            if (!peer.SslStream.CanRead)
            {
                return null;
            }    

            // start reading header
            using (MemoryStream headerMs = new MemoryStream())
            {
                byte[] headerBuffer = new byte[1];//if we change header structure we need to up date this
                timeout = false;
                currentTimeout = 0;
                Int32 read = 0;

                /// start reading header
                while ((read = await peer.SslStream.ReadAsync(headerBuffer, 0, headerBuffer.Length)) > 0)
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
                /// end reading header

                if (timeout)
                {
                    Log.Log.Message("*** MessageReadAsync timeout " + currentTimeout + "ms/" + maxTimeout + "ms exceeded while reading header after reading " + bytesRead + " bytes");
                    return null;
                }

                headerBytes = headerMs.ToArray();
                if (headerBytes == null || headerBytes.Length < 1)
                {
                    return null;
                }

                if (!Int64.TryParse(Encoding.UTF8.GetString(headerBytes).Replace(":", ""), out contentLength))
                {
                    Log.Log.Message("*** MessageReadAsync malformed message from " + peer.Ip + peer.Port + " (message header not an integer)");
                    return null;
                }
            }
            // endreading header
            
            // start reading descriptor chunk
            
            // stop reading descriptor chunk
            
            /// start reading content
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

                while ((read = await peer.SslStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
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
                    Log.Log.Message("*** MessageReadAsync timeout " + currentTimeout + "ms/" + maxTimeout + "ms exceeded while reading content after reading " + bytesRead + " bytes");
                    return null;
                }

                contentBytes = dataMs.ToArray();
            }
            /// end reading content

            if (contentBytes == null || contentBytes.Length < 1)
            {
                Log.Log.Message("*** MessageReadAsync " + peer.Ip + peer.Port + " no content read");
                return null;
            }

            if (contentBytes.Length != contentLength)
            {
                Log.Log.Message("*** MessageReadAsync " + peer.Ip + peer.Port + " content length " + contentBytes.Length + " bytes does not match header value " + contentLength + ", discarding");
                return null;
            }

            byte[] msgDescriptor = ByteUtil.Slice(contentBytes, 0, 3);
            byte[] payload = ByteUtil.Slice(contentBytes, 3);
            return payload;
        }
    }
}