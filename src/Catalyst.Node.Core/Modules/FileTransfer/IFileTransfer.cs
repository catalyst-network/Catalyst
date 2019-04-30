using Catalyst.Common.Rpc;

namespace Catalyst.Node.Core.Modules.FileTransfer
{
    /// <summary>
    /// The File Transfer interface
    /// </summary>
    public interface IFileTransfer
    {
        /// <summary>Initializes the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Initialization response code</returns>
        AddFileToDfsResponseCode InitializeTransfer(FileTransferInformation fileTransferInformation);

        /// <summary>Writes the chunk.</summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <param name="fileChunk">The file chunk.</param>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Writing chunk response code</returns>
        AddFileToDfsResponseCode WriteChunk(string fileName, uint chunkId, byte[] fileChunk);

        /// <summary>Gets the file transfer information.</summary>
        /// <param name="key">The unique file name.</param>
        /// <returns>File transfer information</returns>
        FileTransferInformation GetFileTransferInformation(string key);

        /// <summary>Gets the keys.</summary>
        /// <value>The keys.</value>
        string[] Keys { get; }
    }
}
