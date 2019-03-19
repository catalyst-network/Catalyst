using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace Catalyst.Node.Common.Helpers
{
    public static class ProtobufExtensions
    {
        private const string CatalystProtocol = "Catalyst.Protocol";
        private static readonly Dictionary<string, string> ProtoToClrNameMapper = Assembly.Load(CatalystProtocol).ExportedTypes
           .Where(t => typeof(IMessage).IsAssignableFrom(t))
           .Select(t => ((IMessage)Activator.CreateInstance(t)).Descriptor)
           .ToDictionary(d => d.ShortenedFullName(), d => d.ClrType.FullName);

        public static string ShortenedFullName(this MessageDescriptor descriptor)
        {
            //we don't need to serialise the complete full name of our own types
            return descriptor.FullName.Remove(0, CatalystProtocol.Length + 2);
        }

        public static Any ToAny<T>(this T protobufObject) where T : IMessage
        {
            var wrappedObject = new Any
            {
                TypeUrl = protobufObject.Descriptor.ShortenedFullName(),
                Value = protobufObject.ToByteString()
            };
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
            var type = Type.GetType(ProtoToClrNameMapper[message.TypeUrl]);
            var empty = (IMessage)Activator.CreateInstance(type);
            var innerMessage = empty.Descriptor.Parser.ParseFrom(message.Value);
            return innerMessage;
        }
    }
}
