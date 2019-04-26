using Catalyst.Common.Config;
using Catalyst.Common.FileSystem;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.Rpc;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Catalyst.Cli.FileTransfer
{
    public class CliFileTransfer : IDisposable
    {
        /// <summary>The instance</summary>
        private static CliFileTransfer _instance;

        private uint _currentChunk;

        private uint _maxChunk;

        private RpcMessageFactoryBase<TransferFileBytesRequest, RpcMessages> _rpcMessageFactory;

        /// <summary>Initializes a new instance of the <see cref="CliFileTransfer"/> class.</summary>
        public CliFileTransfer()
        {
            RetryCount = 0;
            WaitHandle = new ManualResetEvent(false);
            _rpcMessageFactory = new RpcMessageFactoryBase<TransferFileBytesRequest, RpcMessages>();
        }

        /// <summary>Waits this instance.</summary>
        /// <returns>False if no signal was recieved, true if signal wait recieved</returns>
        public bool Wait()
        {
            return WaitHandle.WaitOne(TimeSpan.FromSeconds(FileTransferConstants.CliFileTransferWaitTime));
        }

        public void FileTransferCallback(AddFileToDfsResponseCode responseCode)
        {
            CurrentChunkResponse = responseCode;
            WaitHandle.Set();
        }

        /// <summary>The file transfer initialisation response callback.</summary>
        /// <param name="code">The code.</param>
        public void InitialiseFileTransferResponseCallback(AddFileToDfsResponseCode code)
        {
            InitialiseFileTransferResponse = code;
            WaitHandle.Set();
        }

        public void Dispose()
        {
            WaitHandle.Reset();
            InitialiseFileTransferResponse = default;
            CurrentChunkResponse = default;
        }

        /// <summary>Gets or sets the initialise file transfer response.</summary>
        /// <value>The initialise file transfer response.</value>
        public AddFileToDfsResponseCode InitialiseFileTransferResponse { get; set; }

        /// <summary>Gets or sets the current chunk response.</summary>
        /// <value>The current chunk response.</value>
        public AddFileToDfsResponseCode CurrentChunkResponse { get; set; }

        public ManualResetEvent WaitHandle { get; set; }

        /// <summary>Gets or sets the retry count.</summary>
        /// <value>The retry count.</value>
        public int RetryCount { get; set; }

        /// <summary>Gets the instance.</summary>
        /// <value>The instance.</value>
        public static CliFileTransfer Instance
        {
            get
            {
                return (_instance ?? (_instance = new CliFileTransfer()));
            }
        }

        public void TransferFile(string filePath, Guid correlationGuid, INodeRpcClient node, IPeerIdentifier peerIdentifier)
        {
            WaitHandle.Reset();
            
            Console.WriteLine("Transfering file: 0%");
            ByteString correlationBytes = ByteString.CopyFrom(correlationGuid.ToByteArray());

            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            {
                long fileLen = fileStream.Length;

                _currentChunk = 0;
                _maxChunk = (uint) Math.Max(1, (int) Math.Ceiling((double) fileLen / FileTransferConstants.ChunkSize));

                for (uint i = 0; i < _maxChunk; i++)
                {
                    uint chunkId = i + 1;
                    long startPos = i * FileTransferConstants.ChunkSize;
                    long endPos = chunkId * FileTransferConstants.ChunkSize;
                    if (endPos > fileLen)
                    {
                        endPos = fileLen;
                    }

                    int bufferSize = (int) (endPos - startPos);
                    byte[] chunk = new byte[bufferSize];
                    fileStream.Position = startPos;
                    fileStream.Read(chunk, 0, bufferSize);

                    var transferMessage = new TransferFileBytesRequest
                    {
                        ChunkBytes = ByteString.CopyFrom(chunk),
                        ChunkId = chunkId,
                        CorrelationFileName = correlationBytes
                    };

                    var requestMessage = _rpcMessageFactory.GetMessage(new P2PMessageDto<TransferFileBytesRequest, RpcMessages>(
                        type: RpcMessages.TransferFileBytesRequest,
                        message: transferMessage,
                        destination: (IPEndPoint) node.Channel.RemoteAddress,
                        sender: peerIdentifier
                    ));

                    node.SendMessage(requestMessage);

                    bool responseRecieved = Wait();

                    if (!responseRecieved)
                    {
                        bool retrySuccess = Retry(ref i);
                        if (!retrySuccess)
                        {
                            Console.WriteLine("Error transfering file. Node Timeout");
                            break;
                        }
                    }
                    else
                    {
                        bool processSuccess = ProcessChunkResponse(ref i);
                        if (!processSuccess)
                        {
                            Console.WriteLine("Error transfering file. Node Response: " + CurrentChunkResponse);
                            break;
                        }
                    }
                }

                Dispose();
            }
        }

        private bool ProcessChunkResponse(ref uint index)
        {
            if (CurrentChunkResponse == AddFileToDfsResponseCode.Expired)
            {
                return false;
            }
            else if (CurrentChunkResponse == AddFileToDfsResponseCode.Successful)
            {
                _currentChunk = index;
                Console.Write("\rTransfering file: " + GetPercentage() + " %");
                if (_currentChunk == _maxChunk)
                {
                    this.Dispose();
                }

                WaitHandle.Reset();
            }
            else
            {
                return Retry(ref index);
            }

            return true;
        }

        /// <summary>Gets the percentage.</summary>
        /// <returns>Current percentage</returns>
        private int GetPercentage()
        {
            return (int) Math.Ceiling((double) _currentChunk / _maxChunk * 100D);
        }

        private bool Retry(ref uint index)
        {
            if (RetryCount >= FileTransferConstants.MaxChunkRetryCount)
            {
                return false;
            }
            else
            {
                index--;
                return true;
            }
        }
    }
}
