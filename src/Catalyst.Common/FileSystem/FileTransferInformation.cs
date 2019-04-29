using Catalyst.Common.Interfaces.P2P;
using Dawn;
using DotNetty.Transport.Channels;
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

        public FileTransferInformation(IChannel reciepientChannel, string uniqueFileName, string fileName, uint maxChunk)
        {
            _tempPath = Path.GetTempPath() + uniqueFileName;
            _maxChunk = maxChunk;
            this.CurrentChunk = 0;
            this.ReciepientChannel = reciepientChannel;
            this.UniqueFileName = uniqueFileName;
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

        public void CleanUp()
        {
            this.Dispose();
            File.Delete(_tempPath);
        }

        public void Dispose()
        {
            this.RandomAccessStream.Close();
            this.RandomAccessStream.Dispose();
        }

        public Action<FileTransferInformation> OnExpired { get; set; }

        public Action<FileTransferInformation> OnSuccess { get; set; }

        public uint CurrentChunk { get; set; }

        public string DfsHash { get; set; }

        public uint MaxChunk { get => _maxChunk; }

        public string UniqueFileName { get; set; }

        public BinaryWriter RandomAccessStream { get; set; }

        public string TempPath { get => _tempPath; }

        public string FileName { get; set; }
        
        public IChannel ReciepientChannel { get; set; }
    }
}
