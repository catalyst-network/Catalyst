using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using Xunit;
using Catalyst.Node.Common.Exceptions;

namespace Catalyst.Node.Common.UnitTests.Exceptions
{
    public class UnsupportedPlatformExceptionTest
    {
        [Fact]
        public void Test_UnsupportedPlatformException_Message_Excepcted()
        {
            var expectedMessage = "Do not support XYZ";
            var unsupportedPlatformEx = new UnsupportedPlatformException(expectedMessage);
            unsupportedPlatformEx.Message.Should().Be(expectedMessage);
        }

        [Fact]
        public void Test_UnsupportedException_Throws_Exception_When_Message_Null()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new UnsupportedPlatformException(null));
        }

        [Fact]
        public void Test_UnsupportedPlatformException_deserialization()
        {
            var expectedMessage = "Do not support XYZ";
            var unsupportedPlatformEx = new UnsupportedPlatformException(expectedMessage);
            
            var buffer = new byte[4096];
            var ms = new MemoryStream(buffer);
            var ms2 = new MemoryStream(buffer);
            var formatter = new BinaryFormatter();
 
            formatter.Serialize(ms, unsupportedPlatformEx);
            var deserializedException = (UnsupportedPlatformException)formatter.Deserialize(ms2);
            Assert.Equal(unsupportedPlatformEx.Message, deserializedException.Message);
        }
    }
}