using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Node.Common.Helpers;
using Catalyst.Protocols.Transaction;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Helpers
{
    public class ProtobufExtensionsTests
    {
        [Fact]
        public void ShortenedFullName_should_remove_namespace_start()
        {
            StTx.Descriptor.FullName.Should().Be("Catalyst.Protocols.Transaction.StTx");
            StTx.Descriptor.ShortenedFullName().Should().Be("Transaction.StTx");
        }
    }
}
