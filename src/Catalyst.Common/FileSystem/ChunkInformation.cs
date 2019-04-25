using Dawn;
using System;
using System.IO;

namespace Catalyst.Common.FileSystem
{
    public class FileTransferInformation : IDisposable
    {
        /// <summary>The expired time span</summary>
        public static readonly TimeSpan ExpiredTimeSpan = TimeSpan.FromMinutes(1);

        /// <summary>The time since last chunk</summary>
        private DateTime _timeSinceLastChunk;

        /// <summary>The temporary path</summary>
        private readonly string _tempPath;

        /// <summary>The maximum chunk</summary>
        private readonly int _maxChunk;

        public FileTransferInformation(string hash, int maxChunk)
        {
            _tempPath = Path.GetTempPath() + hash;
            CurrentChunk = 0;
            this._maxChunk = maxChunk;
            this.Hash = hash;
            this.RandomAccessStream = new BinaryWriter(File.Open(_tempPath, FileMode.CreateNew));
        }

        public void WriteToStream(int chunk, byte[] fileBytes)
        {
            this.RandomAccessStream.Seek(0, SeekOrigin.End);
            this.RandomAccessStream.Write(fileBytes);
            CurrentChunk = chunk;
            _timeSinceLastChunk = DateTime.Now;
        }

        /// <summary>Determines whether this instance is expired.</summary>
        /// <returns><c>true</c> if this instance is expired; otherwise, <c>false</c>.</returns>
        public bool IsExpired()
        {
            return DateTime.Now.Subtract(_timeSinceLastChunk) > ExpiredTimeSpan;
        }

        public void Dispose()
        {
            this.RandomAccessStream.Close();

        }

        public int CurrentChunk { get; set; }

        public int MaxChunk { get => _maxChunk; }

        public string Hash { get; set; }

        public BinaryWriter RandomAccessStream { get; set; }
    }
}
