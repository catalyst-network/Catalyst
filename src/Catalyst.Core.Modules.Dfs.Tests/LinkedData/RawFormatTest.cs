using System.Text;
using Catalyst.Core.Modules.Dfs.LinkedData;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.LinkedData
{
    public class RawFormatTest
    {
        ILinkedDataFormat formatter = new RawFormat();

        [Fact]
        public void Empty()
        {
            var data = new byte[0];

            var cbor = formatter.Deserialise(data);
            Assert.Equal(data, cbor["data"].GetByteString());

            var data1 = formatter.Serialize(cbor);
            Assert.Equal(data, data1);
        }

        [Fact]
        public void Data()
        {
            var data = Encoding.UTF8.GetBytes("abc");

            var cbor = formatter.Deserialise(data);
            Assert.Equal(data, cbor["data"].GetByteString());

            var data1 = formatter.Serialize(cbor);
            Assert.Equal(data, data1);
        }
    }
}
