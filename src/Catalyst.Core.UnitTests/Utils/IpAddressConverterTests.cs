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
using System.Net;
using System.Text;
using Catalyst.Core.Util;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Catalyst.Core.UnitTests.Utils
{
    public sealed class IpAddressConverterTests
    {
        private readonly IpAddressConverter _ipAddressConverter;

        private sealed class JsonIpTester
        {
            public IPAddress Ip { get; set; }
        }

        public IpAddressConverterTests()
        {
            _ipAddressConverter = new IpAddressConverter();
        }

        [Fact]
        public void Check_Convertibility_Must_Succeed()
        {
            _ipAddressConverter.CanConvert(IPAddress.Loopback.GetType()).Should().BeTrue();
        }

        [Fact]
        public void Check_Convertibility_Should_Fail()
        {
            var ipAddressBytes = Encoding.ASCII.GetBytes("FalseData_Fake_Ip_Address_198.0.yplor");

            _ipAddressConverter.CanConvert(ipAddressBytes.GetType()).Should().BeFalse();
        }

        [Fact]
        public void Write_To_Json_Should_Succeed()
        {
            var ipTestObject = new JsonIpTester {Ip = IPAddress.Parse("127.0.0.1")};
            var serialized = JsonConvert.SerializeObject(ipTestObject, _ipAddressConverter);
            serialized.Should().Contain("\"Ip\":\"127.0.0.1\"");
        }

        [Fact]
        public void Read_Json_Should_Succeed()
        {
            var testJson = "{ \"Ip\": \"127.0.0.1\" }";

            JsonConvert.DeserializeObject<JsonIpTester>(testJson, _ipAddressConverter)
               .Ip.Should().Be(IPAddress.Parse("127.0.0.1"));
        }

        [Fact]
        public void Read_Json_Via_DeserializeObject_Should_Failed()
        {
            var testJson = "{ \"Ip\": \"127.0.0.1/\" }";

            Assert.Throws<FormatException>(() => JsonConvert.DeserializeObject<JsonIpTester>(testJson, _ipAddressConverter));
        }
    }
}
