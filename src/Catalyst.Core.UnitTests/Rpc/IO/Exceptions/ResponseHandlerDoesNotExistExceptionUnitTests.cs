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

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Catalyst.Core.Rpc.IO.Exceptions;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.IO.Exceptions
{
    public sealed class ResponseHandlerDoesNotExistExceptionUnitTests
    {
        [Fact]
        public void ResponseHandlerDoesNotExistException_Should_Be_Serializable()
        {
            var exception =
                new ResponseHandlerDoesNotExistException(
                    "Message");

            var exceptionToString = exception.ToString();

            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, exception);
                memoryStream.Seek(0, 0);
                exception = (ResponseHandlerDoesNotExistException) binaryFormatter.Deserialize(memoryStream);
            }

            exceptionToString.Should().Be(exception.ToString());
        }
    }
}
