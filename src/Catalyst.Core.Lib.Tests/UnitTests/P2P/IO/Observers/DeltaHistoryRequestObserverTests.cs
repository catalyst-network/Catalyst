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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Ledger.Service;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Reactive.Testing;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Observers
{
    public sealed class DeltaHistoryRequestObserverTests : IDisposable
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _subbedLogger;
        private readonly DeltaHistoryRequestObserver _deltaHistoryRequestObserver;

        public DeltaHistoryRequestObserverTests()
        {
            _testScheduler = new TestScheduler();
            _subbedLogger = Substitute.For<ILogger>();
            
            var peerSettings = PeerIdHelper.GetPeerId("sender").ToSubstitutedPeerSettings();
            var deltaIndexService = new DeltaIndexService(new InMemoryRepository<DeltaIndexDao, string>());

            _deltaHistoryRequestObserver = new DeltaHistoryRequestObserver(peerSettings,
                deltaIndexService,
                new TestMapperProvider(), 
                _subbedLogger
            );
        }

        [Fact]
        public async Task Can_Process_DeltaHeightRequest_Correctly()
        {
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var deltaHistoryRequestMessage = new DeltaHistoryRequest();
            
            var channeledAny = new ObserverDto(fakeContext,
                deltaHistoryRequestMessage.ToProtocolMessage(PeerIdHelper.GetPeerId(),
                    CorrelationId.GenerateCorrelationId()
                )
            );
            
            var observableStream = new[] {channeledAny}.ToObservable(_testScheduler);
            
            _deltaHistoryRequestObserver.StartObserving(observableStream);
            _testScheduler.Start();

            var response = new DeltaHistoryResponse();
            var hp = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            var lastDeltaHash = hp.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32));

            for (uint x = 0; x < 10; x++)
            {
                var delta = new Delta
                {
                    PreviousDeltaDfsHash = lastDeltaHash.Digest.ToByteString()
                };

                var index = new DeltaIndex
                {
                    Height = 10,
                    Cid = delta.ToByteString()
                };

                response.Result.Add(index);
                lastDeltaHash = hp.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32));
            }
            
            await fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(response.ToProtocolMessage(PeerIdHelper.GetPeerId(), CorrelationId.GenerateCorrelationId())).ConfigureAwait(false);
            
            _subbedLogger.ReceivedWithAnyArgs(1);
        }
        
        public void Dispose()
        {
            _deltaHistoryRequestObserver?.Dispose();
        }
    }
}
