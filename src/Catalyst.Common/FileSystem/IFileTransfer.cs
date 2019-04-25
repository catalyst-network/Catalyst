using Catalyst.Common.Rpc;

namespace Catalyst.Common.FileSystem
{
    public interface IFileTransfer
    {
        AddFileToDfsResponseCode InitializeTransfer(FileTransferInformation fileTransferInformation);
        AddFileToDfsResponseCode WriteChunk(string fileHash, int chunkId, byte[] fileChunk);
    }
}
