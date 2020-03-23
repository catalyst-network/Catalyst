using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.DAO.Transaction;
using SharpRepository.Repository;
using System.IO;

namespace Catalyst.Core.Modules.Sync
{
    public class StateResetter : IStateResetter
    {
        private readonly IRepository<DeltaIndexDao, string> _deltaIndexes;
        private readonly IRepository<TransactionReceipts, string> _transactionReceipts;
        private readonly IRepository<TransactionToDelta, string> _transactionToDeltas;
        private readonly IRepository<PublicEntryDao, string> _mempool;
        private readonly IFileSystem _fileSystem;

        private readonly Serilog.ILogger _logger;
        public StateResetter(IRepository<DeltaIndexDao, string> deltaIndexes,
            IRepository<TransactionReceipts, string> transactionReceipts,
            IRepository<TransactionToDelta, string> transactionToDeltas,
            IRepository<PublicEntryDao, string> mempool,
            IFileSystem fileSystem,
            Serilog.ILogger logger)
        {
            _deltaIndexes = deltaIndexes;
            _transactionReceipts = transactionReceipts;
            _transactionToDeltas = transactionToDeltas;
            _mempool = mempool;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public void Reset()
        {
            var _targetConfigFolder = _fileSystem.GetCatalystDataDir().FullName;
            _logger.Information("Resetting State");

            var stateFolder = Path.Join(_targetConfigFolder, "state");
            if (Directory.Exists(stateFolder))
            {
                _logger.Information("Deleting EVM State");
                Directory.Delete(stateFolder, true);
            }

            var codeFolder = Path.Join(_targetConfigFolder, "code");
            if (Directory.Exists(codeFolder))
            {
                _logger.Information("Deleting EVM Code");
                Directory.Delete(codeFolder, true);
            }

            var blockFolder = Path.Join(_targetConfigFolder, "dfs", "blocks");
            if (Directory.Exists(blockFolder))
            {
                _logger.Information("Deleting DFS Blocks");
                Directory.Delete(blockFolder, true);
            }

            var pinFolder = Path.Join(_targetConfigFolder, "dfs", "pins");
            if (Directory.Exists(pinFolder))
            {
                _logger.Information("Deleting DFS Pins");
                Directory.Delete(pinFolder, true);
            }

            _logger.Information("Deleting DeltaIndexes");
            foreach (var deltaIndex in _deltaIndexes.GetAll())
            {
                _deltaIndexes.Delete(deltaIndex);
            }

            _logger.Information("Deleting transactionReceipts");
            foreach (var transactionReceipt in _transactionReceipts.GetAll())
            {
                _transactionReceipts.Delete(transactionReceipt);
            }

            _logger.Information("Deleting transactionToDeltas");
            foreach (var transactionToDelta in _transactionToDeltas.GetAll())
            {
                _transactionToDeltas.Delete(transactionToDelta);
            }

            _logger.Information("Deleting mempool");
            foreach (var mempoolItem in _mempool.GetAll())
            {
                _mempool.Delete(mempoolItem);
            }
        }
    }
}
