using FluentAssertions;
using Xunit;
using Catalyst.Node.Common.Exceptions;

namespace Catalyst.Node.Common.UnitTests.Exceptions
{
    public class UnsupportedPlatformExceptionTest
    {
        [Fact]
        public void UnsupportedPlatformTest()
        {
            var unsupportedPlatformEx = new UnsupportedPlatformException("Do not support XYZ");
            unsupportedPlatformEx.Message.Should().Be("Do not support XYZ");
        }
    }
}