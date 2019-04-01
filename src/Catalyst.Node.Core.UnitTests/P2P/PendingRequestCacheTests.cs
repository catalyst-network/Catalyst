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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using FluentAssertions;
using NSubstitute;
using SharpRepository.Repository;
using Xunit;
using PendingRequest = Catalyst.Node.Common.P2P.PendingRequest;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public class PendingRequestCacheTests
    {
        private readonly IRepository<PendingRequest> _responseStore;
        private PeerIdentifier[] _peerIds;
        private readonly IList<PendingRequest> _pendingRequests;
        private PendingRequestCache _cache;

        public PendingRequestCacheTests()
        {
            _responseStore = Substitute.For<IRepository<PendingRequest>>();
            _peerIds = new []
            {
                PeerIdentifierHelper.GetPeerId("abcd"),
                PeerIdentifierHelper.GetPeerId("efgh")
            }.Select(p => new PeerIdentifier(p)).ToArray();

            _pendingRequests = _peerIds.Select((p, i) => new PendingRequest()
            {
                TargetNodeId = p,
                RequestContent = Guid.NewGuid().ToByteArray().ToByteString(),
                SentDateTimeOffset = DateTimeOffset.MinValue
                   .Add(TimeSpan.FromMilliseconds(100 * i))
            }).ToList();

            _cache = new PendingRequestCache(_responseStore);
        }

        [Fact]
        public void TryMatchResponseAsync_should_match_existing_records()
        {
            var responseMatchingNumber2 = new PingResponse()
            {
                CorrelationId = _pendingRequests[1].RequestContent
            };
            _cache.ResponseStore.TryFind(Arg.Any<Expression<Func<PendingRequest, bool>>>(),
                Arg.Any<Expression<Func<PendingRequest, PendingRequest>>>(),
                out PendingRequest x).Returns(ci =>
            {
                ci[2] = responseMatchingNumber2;
                return true;
            });

            var request = _cache.TryMatchResponseAsync(responseMatchingNumber2, _peerIds[1]);
            request.Should().NotBeNull();

            request.RequestContent.Should().Equal(_pendingRequests[1].RequestContent);
        }
    }
}
