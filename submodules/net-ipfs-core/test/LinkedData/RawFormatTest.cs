using System.Text;
using Ipfs.Core.LinkedData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Core.Tests.LinkedData
{
    [TestClass]
    public class RawFormatTest
    {
        ILinkedDataFormat _formatter = new RawFormat();

        [TestMethod]
        public void Empty()
        {
            var data = new byte[0];

            var cbor = _formatter.Deserialise(data);
            CollectionAssert.AreEqual(data, cbor["data"].GetByteString());

            var data1 = _formatter.Serialize(cbor);
            CollectionAssert.AreEqual(data, data1);
        }

        [TestMethod]
        public void Data()
        {
            var data = Encoding.UTF8.GetBytes("abc");

            var cbor = _formatter.Deserialise(data);
            CollectionAssert.AreEqual(data, cbor["data"].GetByteString());

            var data1 = _formatter.Serialize(cbor);
            CollectionAssert.AreEqual(data, data1);
        }
    }
}
