using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Deltas;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Catalyst.Core.Modules.Sync
{
    public interface IDeltaDownloadManager
    {
        void DownloadDeltasAsync(IList<DeltaIndex> deltaIndexes, Action complete);
    }

    public class DeltaDownloadManager : IDeltaDownloadManager
    {
        private readonly IDfsService _dfsService;
        private readonly IHashProvider _hashProvider;
        public DeltaDownloadManager(IDfsService dfsService, IHashProvider hashProvider)
        {
            _dfsService = dfsService;
            _hashProvider = hashProvider;
        }

        public void DownloadDeltasAsync(IList<DeltaIndex> deltaIndexes, Action complete)
        {
            Task.Run(() =>
            {
                Parallel.ForEach(deltaIndexes, async deltaIndex =>
                       {
                           while (true)
                           {
                               try
                               {
                                   var cid = deltaIndex.Cid.ToByteArray().ToCid();
                                   var deltaStream =
                                       await _dfsService.UnixFsApi.ReadFileAsync(cid).ConfigureAwait(false);
                                   await _dfsService.UnixFsApi
                                      .AddAsync(deltaStream, options: new AddFileOptions { Hash = _hashProvider.HashingAlgorithm.Name })
                                      .ConfigureAwait(false);
                                   return;
                               }
                               catch (Exception exc) { }

                               await Task.Delay(100).ConfigureAwait(false);
                           }
                       });

                complete.Invoke();
            });
        }
    }
}
