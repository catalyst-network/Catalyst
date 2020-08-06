#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Lib.P2P;
using Microsoft.Extensions.Caching.Memory;
using Nethermind.Core;
using MultiFormats;
using Nethermind.Db;
using Nethermind.State;
using Serilog;
using Catalyst.Core.Lib.DAO.Ledger;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc cref="IDeltaCache" />
    /// <inheritdoc cref="IDisposable" />
    public class DeltaCache : IDeltaCache, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDeltaDfsReader _dfsReader;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly ILogger _logger;
        private readonly Func<MemoryCacheEntryOptions> _entryOptions;
        public Cid GenesisHash { get; set; }

        public static string GetLocalDeltaCacheKey(CandidateDeltaBroadcast candidate) { return nameof(DeltaCache) + "-LocalDelta-" + MultiBase.Encode(candidate.Hash.ToByteArray(), "base32"); }

        public static Address TruffleTestAccount = new Address("0xb77aec9f59f9d6f39793289a09aea871932619ed");

        public static Address CatalystTruffleTestAccount = new Address("0x58BeB247771F0B6f87AA099af479aF767CcC0F00");
        //{{ "receiverAddress": "AA==", "senderAddress": "1vw3HRttl8xHoRJcE6oO1q5cjoGOFNv/Db8d4oWAdT8=", "amount": "AAAAAAAAAAA=", "data": "YIBgQFIzYACAYQEACoFUgXP//////////////////////////wIZFpCDc///////////////////////////FgIXkFVQYBRgAVU0gBViAABWV2AAgP1bUGBAUWIAGX44A4BiABl+gzmBAYBgQFKBAZCAgFGCAZKRkFBQUIBgAIFgA5CAUZBgIAGQYgAAlpKRkGIAAflWW1BgAJBQW4FRgRAVYgABhldgAWAEYACEhIFRgRAVFWIAALtX/luQYCABkGAgAgFRc///////////////////////////FnP//////////////////////////xaBUmAgAZCBUmAgAWAAIGAAAWAAYQEACoFUgWD/AhkWkIMVFQIXkFVQgGAEYACEhIFRgRAVFWIAAS1X/luQYCABkGAgAgFRc///////////////////////////FnP//////////////////////////xaBUmAgAZCBUmAgAWAAIGABAYGQVVCAgGABAZFQUGIAAJxWW2ADYAKQgFRiAAGakpGQYgACiFZbUFBQc//////////////////////////+YAVgAGEBAAqBVIFz//////////////////////////8CGRaQg3P//////////////////////////xYCF5BVUFBiAAMlVluCgFSCglWQYABSYCBgACCQgQGSghViAAJ1V5FgIAKCAVuCgREVYgACdFeCUYJgAGEBAAqBVIFz//////////////////////////8CGRaQg3P//////////////////////////xYCF5BVUJFgIAGRkGABAZBiAAIaVltbUJBQYgAChJGQYgAC31ZbUJBWW4KAVIKCVZBgAFJgIGAAIJCBAZKCFWIAAsxXYABSYCBgACCRggFbgoERFWIAAstXglSCVZFgAQGRkGABAZBiAAKuVltbUJBQYgAC25GQYgAC31ZbUJBWW2IAAyKRkFuAghEVYgADHldgAIGBYQEACoFUkHP//////////////////////////wIZFpBVUGABAWIAAuZWW1CQVluQVlthFkmAYgADNWAAOWAA8wBggGBAUmAENhBhAMVXYAA1fAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkARj/////xaAYwyFeAUUYQDKV4BjEa6e0hRhAPVXgGMTr0A1FGEBYVeAY0ChQf8UYQGkV4BjTSOMjhRhAedXgGN1KGIRFGECKleAY4yQrAcUYQJBV4BjjaXLWxRhAm5XgGOz8FuXFGECxVeAY7erTbUUYQL0V4BjxHbdQBRhA2BXgGPT6EjxFGEDxVeAY9afE7sUYQQcV1tgAID9WzSAFWEA1ldgAID9W1BhAN9hBGlWW2BAUYCCgVJgIAGRUFBgQFGAkQOQ81s0gBVhAQFXYACA/VtQYQEKYQRvVltgQFGAgGAgAYKBA4JSg4GBUYFSYCABkVCAUZBgIAGQYCACgIODYABbg4EQFWEBTVeAggFRgYQBUmAggQGQUGEBMlZbUFBQUJBQAZJQUFBgQFGAkQOQ81s0gBVhAW1XYACA/VtQYQGiYASANgOBAZCAgDVz//////////////////////////8WkGAgAZCSkZBQUFBhBP1WWwBbNIAVYQGwV2AAgP1bUGEB5WAEgDYDgQGQgIA1c///////////////////////////FpBgIAGQkpGQUFBQYQYWVlsAWzSAFWEB81dgAID9W1BhAihgBIA2A4EBkICANXP//////////////////////////xaQYCABkJKRkFBQUGEJv1ZbAFs0gBVhAjZXYACA/VtQYQI/YQuQVlsAWzSAFWECTVdgAID9W1BhAmxgBIA2A4EBkICANZBgIAGQkpGQUFBQYQv2VlsAWzSAFWECeldgAID9W1BhAoNhDFtWW2BAUYCCc///////////////////////////FnP//////////////////////////xaBUmAgAZFQUGBAUYCRA5DzWzSAFWEC0VdgAID9W1BhAtphDIBWW2BAUYCCFRUVFYFSYCABkVBQYEBRgJEDkPNbNIAVYQMAV2AAgP1bUGEDCWEMk1ZbYEBRgIBgIAGCgQOCUoOBgVGBUmAgAZFQgFGQYCABkGAgAoCDg2AAW4OBEBVhA0xXgIIBUYGEAVJgIIEBkFBhAzFWW1BQUFCQUAGSUFBQYEBRgJEDkPNbNIAVYQNsV2AAgP1bUGEDw2AEgDYDgQGQgIA1c///////////////////////////FpBgIAGQkpGQgDWQYCABkJKRkIA1kGAgAZCCAYA1kGAgAZGQkZKTkZKTkFBQUGENIVZbAFs0gBVhA9FXYACA/VtQYQPaYQ1lVltgQFGAgnP//////////////////////////xZz//////////////////////////8WgVJgIAGRUFBgQFGAkQOQ81s0gBVhBChXYACA/VtQYQRnYASANgOBAZCAgDVz//////////////////////////8WkGAgAZCSkZCANZBgIAGQkpGQUFBQYQ2LVlsAW2ABVIFWW2BgYAOAVIBgIAJgIAFgQFGQgQFgQFKAkpGQgYFSYCABgoBUgBVhBPNXYCACggGRkGAAUmAgYAAgkFuBYACQVJBhAQAKkARz//////////////////////////8Wc///////////////////////////FoFSYCABkGABAZCAgxFhBKlXW1BQUFBQkFCQVltgAICQVJBhAQAKkARz//////////////////////////8Wc///////////////////////////FjNz//////////////////////////8WFBUVYQVYV2AAgP1bgHP//////////////////////////xZgAICQVJBhAQAKkARz//////////////////////////8Wc///////////////////////////Fn9wrqjYSOipD7dmGyJ9xSLrY5XD2scbY8tZ7dXJiZsjZGBAUWBAUYCRA5CjgGAAgGEBAAqBVIFz//////////////////////////8CGRaQg3P//////////////////////////xYCF5BVUFBWW2AAgGAAkFSQYQEACpAEc///////////////////////////FnP//////////////////////////xYzc///////////////////////////FhQVFWEGc1dgAID9W4FgAIBgBGAAhHP//////////////////////////xZz//////////////////////////8WgVJgIAGQgVJgIAFgACBgAAFgAJBUkGEBAAqQBGD/FpFQYARgAIRz//////////////////////////8Wc///////////////////////////FoFSYCABkIFSYCABYAAgYAEBVJBQgYAVYQcfV1BgAoBUkFCBEFuAFWEHj1dQgnP//////////////////////////xZgAoKBVIEQFRVhB0xX/luQYABSYCBgACABYACQVJBhAQAKkARz//////////////////////////8Wc///////////////////////////FhRbFRVhB5pXYACA/VtgBGAAhnP//////////////////////////xZz//////////////////////////8WgVJgIAGQgVJgIAFgACBgAQFUk1BgA2ABYAOAVJBQA4FUgRAVFWEH9lf+W5BgAFJgIGAAIAFgAJBUkGEBAAqQBHP//////////////////////////xZgA4WBVIEQFRVhCDBX/luQYABSYCBgACABYABhAQAKgVSBc///////////////////////////AhkWkINz//////////////////////////8WAheQVVCDYARgAGADh4FUgRAVFWEIjFf+W5BgAFJgIGAAIAFgAJBUkGEBAAqQBHP//////////////////////////xZz//////////////////////////8Wc///////////////////////////FoFSYCABkIFSYCABYAAgYAEBgZBVUGADYAFgA4BUkFADgVSBEBUVYQkPV/5bkGAAUmAgYAAgAWAAYQEACoFUkHP//////////////////////////wIZFpBVYAOAVICRkGABkANhCVGRkGEVN1ZbUGAEYACGc///////////////////////////FnP//////////////////////////xaBUmAgAZCBUmAgAWAAIGAAgIIBYABhAQAKgVSQYP8CGRaQVWABggFgAJBVUFBhCbhhDZpWW1BQUFBQVltgAICQVJBhAQAKkARz//////////////////////////8Wc///////////////////////////FjNz//////////////////////////8WFBUVYQoaV2AAgP1bgGAEYACCc///////////////////////////FnP//////////////////////////xaBUmAgAZCBUmAgAWAAIGAAAWAAkFSQYQEACpAEYP8WFRUVYQp3V2AAgP1bYAFgBGAAhHP//////////////////////////xZz//////////////////////////8WgVJgIAGQgVJgIAFgACBgAAFgAGEBAAqBVIFg/wIZFpCDFRUCF5BVUGADgFSQUGAEYACEc///////////////////////////FnP//////////////////////////xaBUmAgAZCBUmAgAWAAIGABAYGQVVBgA4KQgGABgVQBgIJVgJFQUJBgAYIDkGAAUmAgYAAgAWAAkJGSkJGQkWEBAAqBVIFz//////////////////////////8CGRaQg3P//////////////////////////xYCF5BVUFBhC4xhDZpWW1BQVltgBWAAkFSQYQEACpAEc///////////////////////////FnP//////////////////////////xYzc///////////////////////////FhQVFWEL7FdgAID9W2EL9GEN2VZbVltgAICQVJBhAQAKkARz//////////////////////////8Wc///////////////////////////FjNz//////////////////////////8WFBUVYQxRV2AAgP1bgGABgZBVUFBWW2AAgJBUkGEBAAqQBHP//////////////////////////xaBVltgAGAUkFSQYQEACpAEYP8WgVZbYGBgAoBUgGAgAmAgAWBAUZCBAWBAUoCSkZCBgVJgIAGCgFSAFWENF1dgIAKCAZGQYABSYCBgACCQW4FgAJBUkGEBAAqQBHP//////////////////////////xZz//////////////////////////8WgVJgIAGQYAEBkICDEWEMzVdbUFBQUFCQUJBWW2ENXzOFhYWFgIBgHwFgIICRBAJgIAFgQFGQgQFgQFKAk5KRkIGBUmAgAYODgIKEN4IBkVBQUFBQUGEO0VZbUFBQUFZbYAVgAJBUkGEBAAqQBHP//////////////////////////xaBVlthDZYzg4NhEalWW1BQVltgAGAUkFSQYQEACpAEYP8WFRVhDbVXYACA/VtgAIBgFGEBAAqBVIFg/wIZFpCDFRUCF5BVUGEN12EUgFZbVltgAGAUkFSQYQEACpAEYP8WFRUVYQ31V2AAgP1bYANgApCAVGEOB5KRkGEVY1ZbUGABYABgFGEBAAqBVIFg/wIZFpCDFRUCF5BVUH+FZM1imxX0fcMQ1FvL/JvPVCCw1RvwZZoWxn+R0nYyU2ACYEBRgIBgIAGCgQOCUoOBgVSBUmAgAZFQgFSAFWEOwVdgIAKCAZGQYABSYCBgACCQW4FgAJBUkGEBAAqQBHP//////////////////////////xZz//////////////////////////8WgVJgIAGQYAEBkICDEWEOd1dbUFCSUFBQYEBRgJEDkKFWW4NgAIBgBGAAhHP//////////////////////////xZz//////////////////////////8WgVJgIAGQgVJgIAFgACBgAAFgAJBUkGEBAAqQBGD/FpFQYARgAIRz//////////////////////////8Wc///////////////////////////FoFSYCABkIFSYCABYAAgYAEBVJBQgYAVYQ99V1BgAoBUkFCBEFuAFWEP7VdQgnP//////////////////////////xZgAoKBVIEQFRVhD6pX/luQYABSYCBgACABYACQVJBhAQAKkARz//////////////////////////8Wc///////////////////////////FhRbFRVhD/hXYACA/VuFYACAYARgAIRz//////////////////////////8Wc///////////////////////////FoFSYCABkIFSYCABYAAgYAABYACQVJBhAQAKkARg/xaRUGAEYACEc///////////////////////////FnP//////////////////////////xaBUmAgAZCBUmAgAWAAIGABAVSQUIGAFWEQpFdQYAKAVJBQgRBbgBVhERRXUIJz//////////////////////////8WYAKCgVSBEBUVYRDRV/5bkGAAUmAgYAAgAWAAkFSQYQEACpAEc///////////////////////////FnP//////////////////////////xYUWxUVYREfV2AAgP1bh2ABVIEBQxEVgBVhETNXUEOBEFsVFWERPldgAID9W2ABFRWKc///////////////////////////Foxz//////////////////////////8WfzLHi2FAxGdFpG6IzYg3B9cNvS8G0T3Xb+X0mcASkNpPYEBRYEBRgJEDkKRQUFBQUFBQUFBQUFZbgmAAgGAEYACEc///////////////////////////FnP//////////////////////////xaBUmAgAZCBUmAgAWAAIGAAAWAAkFSQYQEACpAEYP8WkVBgBGAAhHP//////////////////////////xZz//////////////////////////8WgVJgIAGQgVJgIAFgACBgAQFUkFCBgBVhElVXUGACgFSQUIEQW4AVYRLFV1CCc///////////////////////////FmACgoFUgRAVFWESglf+W5BgAFJgIGAAIAFgAJBUkGEBAAqQBHP//////////////////////////xZz//////////////////////////8WFFsVFWES0FdgAID9W4RgAIBgBGAAhHP//////////////////////////xZz//////////////////////////8WgVJgIAGQgVJgIAFgACBgAAFgAJBUkGEBAAqQBGD/FpFQYARgAIRz//////////////////////////8Wc///////////////////////////FoFSYCABkIFSYCABYAAgYAEBVJBQgYAVYRN8V1BgAoBUkFCBEFuAFWET7FdQgnP//////////////////////////xZgAoKBVIEQFRVhE6lX/luQYABSYCBgACABYACQVJBhAQAKkARz//////////////////////////8Wc///////////////////////////FhRbFRVhE/dXYACA/VuGYAFUgQFDERWAFWEUC1dQQ4EQWxUVYRQWV2AAgP1bYAAVFYlz//////////////////////////8Wi3P//////////////////////////xZ/MseLYUDEZ0WkbojNiDcH1w29LwbRPddv5fSZwBKQ2k9gQFFgQFGAkQOQpFBQUFBQUFBQUFBWW2ABQwNAYAAZFn9VJS+m7uR0G04kp0pw6cEf0sIoHfjW6hMSb/hF94JciWADYEBRgIBgIAGCgQOCUoOBgVSBUmAgAZFQgFSAFWEVJ1dgIAKCAZGQYABSYCBgACCQW4FgAJBUkGEBAAqQBHP//////////////////////////xZz//////////////////////////8WgVJgIAGQYAEBkICDEWEU3VdbUFCSUFBQYEBRgJEDkKJWW4FUgYNVgYERFWEVXleBg2AAUmAgYAAgkYIBkQFhFV2RkGEVtVZbW1BQUFZbgoBUgoJVkGAAUmAgYAAgkIEBkoIVYRWkV2AAUmAgYAAgkYIBW4KBERVhFaNXglSCVZFgAQGRkGABAZBhFYhWW1tQkFBhFbGRkGEV2lZbUJBWW2EV15GQW4CCERVhFdNXYACBYACQVVBgAQFhFbtWW1CQVluQVlthFhqRkFuAghEVYRYWV2AAgYFhAQAKgVSQc///////////////////////////AhkWkFVQYAEBYRXgVltQkFZbkFYAoWVienpyMFggFonvQ1NTzpnob6SWhxPrxKHGyddtkSlOGlGr3vZGHaUAKQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAC3euyfWfnW85eTKJoJrqhxkyYZ7Q==", "gasPrice": "AMgXqAQAAAA=", "gasLimit": "6721975", "nonce": "4", "signature": { "signingContext": { "networkType": "TESTNET", "signatureType": "TRANSACTION_PUBLIC" }, "rawBytes": "tez8RXzwgySCYnzHGC6N0SDHlus7AlYfz7Ip+tlyALiPitKke3N/Hy29o2NZViFvis4mnM18qHVHqQ3iqwXwAg==" } }}

        public DeltaCache(IHashProvider hashProvider,
            IMemoryCache memoryCache,
            IDeltaDfsReader dfsReader,
            IDeltaCacheChangeTokenProvider changeTokenProvider,
            IStorageProvider storageProvider,
            IStateProvider stateProvider,
            ISnapshotableDb stateDb,
            IDeltaIndexService deltaIndexService,
            ILogger logger)
        {
            _deltaIndexService = deltaIndexService;

            stateProvider.CreateAccount(TruffleTestAccount, 1_000_000_000.Kat());
            stateProvider.CreateAccount(CatalystTruffleTestAccount, 1_000_000_000.Kat());
            
            storageProvider.Commit();
            stateProvider.Commit(CatalystGenesisSpec.Instance);

            storageProvider.CommitTrees();
            stateProvider.CommitTree();

            stateDb.Commit();

            var genesisDelta = new Delta
            {
                TimeStamp = Timestamp.FromDateTime(DateTime.UnixEpoch),
                StateRoot = ByteString.CopyFrom(stateProvider.StateRoot.Bytes),
            };

            
            GenesisHash = hashProvider.ComputeMultiHash(genesisDelta).ToCid();

            _dfsReader = dfsReader;
            _logger = logger;
            _entryOptions = () => new MemoryCacheEntryOptions()
               .AddExpirationToken(changeTokenProvider.GetChangeToken())
               .RegisterPostEvictionCallback(EvictionCallback);

            _memoryCache = memoryCache;
            _memoryCache.Set(GenesisHash, genesisDelta);
        }

        private void EvictionCallback(object key, object value, EvictionReason reason, object state) { _logger.Debug("Evicted Delta {0} from cache.", key); }

        /// <inheritdoc />
        public bool TryGetOrAddConfirmedDelta(Cid cid,
            out Delta delta,
            CancellationToken cancellationToken = default)
        {
            //this calls for a TryGetOrCreate IMemoryCache extension function
            if (_memoryCache.TryGetValue(cid, out delta))
            {
                return true;
            }

            if (!_dfsReader.TryReadDeltaFromDfs(cid, out delta, cancellationToken))
            {
                return false;
            }

            _memoryCache.Set(cid, delta, _entryOptions());
            return true;
        }

        public bool TryGetLocalDelta(CandidateDeltaBroadcast candidate, out Delta delta)
        {
            var tryGetLocalDelta = _memoryCache.TryGetValue(GetLocalDeltaCacheKey(candidate), out delta);
            _logger.Verbose("Retrieved full details {delta}", delta?.ToString() ?? "nothing");
            return tryGetLocalDelta;
        }

        public void AddLocalDelta(Cid cid, Delta delta)
        {
            _logger.Verbose("Adding local details of delta with CID {cid}", cid);
            _memoryCache.Set(cid, delta, _entryOptions());
            if (!TryGetOrAddConfirmedDelta(cid, out Delta retrieved, CancellationToken.None))
            {
                throw new InvalidOperationException();
            }
        }

        public void AddLocalDelta(CandidateDeltaBroadcast localCandidate, Delta delta)
        {
            _logger.Verbose("Adding full details of candidate delta {candidate}", localCandidate);
            _memoryCache.Set(GetLocalDeltaCacheKey(localCandidate), delta, _entryOptions());
        }

        protected virtual void Dispose(bool disposing) { _memoryCache.Dispose(); }

        public void Dispose() { Dispose(true); }
    }
}
