using System;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Catalyst.Node.Rpc.Client.UnitTests.Stub
{
    public class TestResponse : IMessage<TestResponse>
    {
        public MessageDescriptor Descriptor => NSubstitute.Substitute.For<MessageDescriptor>();

        public int CalculateSize() { throw new NotImplementedException(); }

        public TestResponse Clone()
        {
            throw new NotImplementedException();
        }

        public bool Equals(TestResponse other)
        {
            throw new NotImplementedException();
        }

        public void MergeFrom(TestResponse message) { throw new NotImplementedException(); }
        public void MergeFrom(CodedInputStream input) { throw new NotImplementedException(); }

        public void WriteTo(CodedOutputStream output) { throw new NotImplementedException(); }
    }
}
