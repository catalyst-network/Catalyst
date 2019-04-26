using Dawn;
using System;
using System.IO;

namespace Catalyst.Common.FileSystem
{
    public class FileTransferInformation : IDisposable
    {
        /// <summary>The time since last chunk</summary>
        private DateTime _timeSinceLastChunk;

        /// <summary>The temporary path</summary>
        private readonly string _tempPath;

        /// <summary>The maximum chunk</summary>
        private readonly uint _maxChunk;

        public FileTransferInformation(string hash, string fileName, uint maxChunk)
        {
            _tempPath = Path.GetTempPath() + hash;
            _maxChunk = maxChunk;
            this.CurrentChunk = 0;
            this.Hash = hash;
            this.FileName = fileName;
        }

        public void WriteToStream(uint chunk, byte[] fileBytes)
        {
            this.RandomAccessStream.Seek(0, SeekOrigin.End);
            this.RandomAccessStream.Write(fileBytes);
            this.CurrentChunk = chunk;
            _timeSinceLastChunk = DateTime.Now;
        }

        public void Init()
        {
            this.RandomAccessStream = new BinaryWriter(File.Open(_tempPath, FileMode.CreateNew));
            _timeSinceLastChunk = DateTime.Now;
        }

        /// <summary>Determines whether this instance is expired.</summary>
        /// <returns><c>true</c> if this instance is expired; otherwise, <c>false</c>.</returns>
        public bool IsExpired()
        {
            return DateTime.Now.Subtract(_timeSinceLastChunk).TotalMinutes > FileTransferConstants.ExpiryMinutes;
        }

        public bool IsComplete()
        {
            return this.CurrentChunk == this.MaxChunk;
        }

        public void CleanUpExpired()
        {
            this.Dispose();
            File.Delete(_tempPath);
        }

        public void Dispose()
        {
            this.RandomAccessStream.Close();
            this.RandomAccessStream.Dispose();
        }

        public Action OnExpired { get; set; }

        public Action OnSuccess { get; set; }

        public uint CurrentChunk { get; set; }

        public uint MaxChunk { get => _maxChunk; }

        public string Hash { get; set; }

        public BinaryWriter RandomAccessStream { get; set; }

        public string TempPath { get => _tempPath; }

        public string FileName { get; set; }
    }
}
