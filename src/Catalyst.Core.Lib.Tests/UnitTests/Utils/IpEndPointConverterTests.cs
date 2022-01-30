#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System.Net;
using Catalyst.Core.Lib.Util;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.Utils
{
    public sealed class IpEndPointConverterTests
    {
        private readonly IpEndPointConverter _ipEndPointConverter;
        private readonly IpAddressConverter _ipAddressConverter;
        private readonly IPEndPoint _ipEndpoint;
        private readonly string _ipEndPointJson;

        public IpEndPointConverterTests()
        {
            _ipEndPointConverter = new IpEndPointConverter();
            _ipAddressConverter = new IpAddressConverter();
            _ipEndpoint = new IPEndPoint(IPAddress.Loopback, 1000);
            _ipEndPointJson = "{\"Address\":\"127.0.0.1\",\"Port\":1000}";
        }

        [Test]
        public void Check_Convertibility_Must_Succeed()
        {
            _ipEndPointConverter.CanConvert(_ipEndpoint.GetType()).Should().BeTrue();
        }

        [Test]
        public void Check_Convertibility_Should_Fail()
        {
            _ipEndPointConverter.CanConvert("bad type".GetType()).Should().BeFalse();
        }

        [Test]
        public void Write_To_Json_Should_Succeed()
        {
            var serialized = JsonConvert.SerializeObject(_ipEndpoint, _ipEndPointConverter, _ipAddressConverter);
            serialized.Should().Contain(_ipEndPointJson);
        }

        [Test]
        public void Read_Json_Should_Succeed()
        {
            JsonConvert.DeserializeObject<IPEndPoint>(_ipEndPointJson, _ipEndPointConverter, _ipAddressConverter).Should().Be(_ipEndpoint);
        }
    }
}
