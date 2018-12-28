using System;
using System.IO;
using System.Net.Security;
using System.Threading.Tasks;
using System.Text;
using ADL.Util;

namespace ADL.Node.Core.Modules.Network.Stream
{
    public static class Reader
    {
        /// <summary>
        /// @TODO in here we need to collect stats of bytes reade, we can also have a counter for requests from a connection and if it is too much block them
        /// </summary>
        /// <param name="sslStream"></param>
        /// <returns></returns>
        public static byte[] MessageRead(SslStream sslStream)
        {
            int bytesRead = 0;
            int currentTimeout;
            long contentLength;
            byte[] contentBytes;
            const int maxTimeout = 500;
            bool timeout = false;
            const int sleepInterval = 25; 

            if (!sslStream.CanRead)
            {
                throw new Exception("cant read stream");
            }    

            // start reading header
            using (MemoryStream headerMs = new MemoryStream())
            {
                byte[] headerBuffer = new byte[1];//if we change header structure we need to up date this
                currentTimeout = 0;
                Int32 read;

                // start reading header
                while ((read = sslStream.Read(headerBuffer, 0, headerBuffer.Length)) > 0)
                {
                    if (read > 0)
                    {
                        headerMs.Write(headerBuffer, 0, read);
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
                        currentTimeout += sleepInterval;
                        Task.Delay(sleepInterval);
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
                        currentTimeout += sleepInterval;
                        Task.Delay(sleepInterval);
                    }
                }
                if (timeout)
                {
                    throw new Exception("MessageReadAsync timeout exceeded while reading header after reading");
                }
                
                var headerBytes = headerMs.ToArray();
                
                // if goes into null we return null and wait and go back in this causes a dos see connection manager DataReciever()
                if (headerBytes == null || headerBytes.Length < 1)
                {
                    return null;
                }
                if (!Int64.TryParse(ByteUtil.ByteToString(headerBytes).Replace(":", ""), out contentLength))
                {
                    throw new Exception("MessageReadAsync malformed message, message header not an integer");
                }
            }
            
            using (MemoryStream dataMs = new MemoryStream())
            {
                int read;
                currentTimeout = 0;
                long bufferSize = 2048;
                long bytesRemaining = contentLength;
             
                if (bufferSize > bytesRemaining)
                {
                    bufferSize = bytesRemaining;
                }

                var buffer = new byte[bufferSize];

                while ((read = sslStream.Read(buffer, 0, buffer.Length)) > 0)
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
                        currentTimeout += sleepInterval;
                        Task.Delay(sleepInterval);
                    }
                }
                if (timeout)
                {
                    throw new Exception("MessageReadAsync timeout exceeded while reading header after reading");
                }
                contentBytes = dataMs.ToArray();
            }
            if (contentBytes == null || contentBytes.Length < 1)
            {
                return null;
            }
            if (contentBytes.Length != contentLength)
            {
                throw new Exception("message descriptor error,  bytes does not match header value");
            }
            return contentBytes;
        }
    }
}
