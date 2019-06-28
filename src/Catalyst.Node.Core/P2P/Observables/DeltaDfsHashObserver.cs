using System;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Delta;
using Catalyst.Protocol.zDelta;
using Multiformats.Hash;
using Serilog;

namespace Catalyst.Node.Core.P2P.Observables
{
    public class DeltaDfsHashObserver : BroadcastObserverBase<DeltaDfsHashBroadcast>, IP2PMessageObserver
    {
        private readonly IDeltaHashProvider _deltaHashProvider;

        public DeltaDfsHashObserver(IDeltaHashProvider deltaHashProvider, ILogger logger) : base(logger)
        {
            _deltaHashProvider = deltaHashProvider;
        }

        public override void HandleBroadcast(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<DeltaDfsHashBroadcast>();
                var previousHash = Multihash.Decode(deserialised.PreviousDeltaDfsHash.ToByteArray());
                var newHash = Multihash.Decode(deserialised.DeltaDfsHash.ToByteArray());
                _deltaHashProvider.TryUpdateLatestHash(previousHash.ToString(), newHash.ToString());
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to update latest delta hash from incoming broadcast message.");
            }
        }
    }
}
