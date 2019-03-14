using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public static class ProtobufExtensions
    {
        public static Any ToAny<T>(this T protobufObject) where T : IMessage
        {
            var wrappedObject = new Any() { TypeUrl = typeof(T).FullName, Value = protobufObject.ToByteString() };
            return wrappedObject;
        }

        public static T FromAny<T>(this Any message) where T : IMessage
        {
            var empty = (T)Activator.CreateInstance(typeof(T));
            var typed = (T)empty.Descriptor.Parser.ParseFrom(message.Value);
            return typed;
        }

        public static IMessage FromAny(this Any message)
        {
            var type = Type.GetType(message.TypeUrl);
            var empty = (IMessage)Activator.CreateInstance(type);
            var innerMessage = empty.Descriptor.Parser.ParseFrom(message.Value);
            return innerMessage;
        }
    }
}
