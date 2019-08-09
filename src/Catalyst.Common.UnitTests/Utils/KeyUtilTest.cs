using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Util;
using FluentAssertions;
using Xunit;

namespace Catalyst.Common.UnitTests.Utils
{
    public class KeyUtilTest
    {
        [Fact]
        public void Can_Resolve_Bytes_Correctly()
        {
            string publicKey = "1AZQ77DaBufYeaKGtrfw2rQuAyUFFoxC6jnwnr6bF8zuMF";
            publicKey.KeyToBytes().KeyToString().Should().Be(publicKey);
        }
    }
}
