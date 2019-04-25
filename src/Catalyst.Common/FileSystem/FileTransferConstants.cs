using System;

namespace Catalyst.Common.FileSystem
{
    public static class FileTransferConstants
    {
        /// <summary>The expiry minutes</summary>
        public const int ExpiryMinutes = 1;

        /// <summary>The expired time span</summary>
        public static readonly TimeSpan ExpiredTimeSpan = TimeSpan.FromMinutes(ExpiryMinutes);

        /// <summary>The chunk size</summary>
        public const int ChunkSize = 1000000;
    }
}
