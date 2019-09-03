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
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport.Channels
{
    public sealed class ObservableChannelTests
    {
        public ObservableChannelTests()
        {
            _channel = Substitute.For<IChannel>();
            var messageSubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1);
            _messageStream = messageSubject.AsObservable();
            _observableChannel = new ObservableChannel(_messageStream, _channel);
        }

        private readonly IChannel _channel;
        private readonly IObservable<IObserverDto<ProtocolMessage>> _messageStream;
        private readonly ObservableChannel _observableChannel;

        [Fact]
        public void MessageStream_Should_Not_Be_Null()
        {
            var exception = Record.Exception(() => new ObservableChannel(null, _channel));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Get_Channel_Should_Return_Correct_Channel() { _observableChannel.Channel.Should().Be(_channel); }

        [Fact]
        public void Get_MessageStream_Should_Return_Correct_MessageStream()
        {
            _observableChannel.MessageStream.Should().Be(_messageStream);
        }

        [Fact]
        public void StartAsync_Should_Return_Completed_Task()
        {
            var completedTask = _observableChannel.StartAsync();
            completedTask.Status.Should().Be(TaskStatus.RanToCompletion);
        }
    }
}
