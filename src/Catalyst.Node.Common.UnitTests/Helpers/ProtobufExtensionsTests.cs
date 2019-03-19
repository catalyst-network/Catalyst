using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Node.Common.Helpers;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Helpers
{
    public class ProtobufExtensionsTests
    {
        [Fact]
        public static void ShortenedFullName_should_remove_namespace_start()
        {
            Transaction.Descriptor.FullName.Should().Be("Catalyst.Protocol.Transaction.Transaction");
            Transaction.Descriptor.ShortenedFullName().Should().Be("Transaction.Transaction");
        }
    }
}
