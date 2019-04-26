using System;

namespace Catalyst.Common.FileSystem
{
    public static class FileTransferConstants
    {
        /// <summary>The expiry minutes</summary>
        public const int ExpiryMinutes = 1;

        /// <summary>The chunk size</summary>
        public const int ChunkSize = 1000000;

        /// <summary>The cli file transfer wait time</summary>
        public const int CliFileTransferWaitTime = 30;

        /// <summary>The maximum chunk retry count</summary>
        public const int MaxChunkRetryCount = 3;
    }
}
