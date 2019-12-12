using System;
using Catalyst.Abstractions.Kvm.Models;
using Catalyst.Abstractions.Ledger;
using Catalyst.Protocol.Deltas;
using LibP2P;

namespace Catalyst.Core.Modules.Web3 
{
    public static class Web3EthApiExtensions
    {
        public static Delta GetLatestDelta(this IWeb3EthApi api)
        {
            return GetDelta(api, api.DeltaResolver.LatestDelta);
        }

        public static Delta GetDelta(this IWeb3EthApi api, Cid cid)
        {
            if (!api.DeltaCache.TryGetOrAddConfirmedDelta(cid, out Delta delta))
            {
                throw new Exception($"Delta not found '{cid}'");
            }

            return delta;
        }

        public static Delta GetDelta(this IWeb3EthApi api, BlockParameter block)
        {
            Cid cid;
            switch (block.Type)
            {
                case BlockParameterType.Earliest:
                    cid = api.DeltaCache.GenesisHash;
                    break;
                case BlockParameterType.Latest:
                    cid = api.DeltaResolver.LatestDelta;
                    break;
                case BlockParameterType.Pending:
                    cid = api.DeltaResolver.LatestDelta;
                    break;
                case BlockParameterType.BlockNumber:
                    var blockNumber = block.BlockNumber.Value;
                    cid = api.DeltaResolver.Resolve(blockNumber);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return api.GetDelta(cid);
        }
    }
}
