/*
* Copyright(c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node<https: //github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node.If not, see<https: //www.gnu.org/licenses/>.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawn;
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
            return descriptor.FullName.Remove(0, CatalystProtocol.Length + 1);
        }

        public static string ShortenedProtoFullName(this Type protoType)
        {
            Guard.Argument(protoType, nameof(protoType)).Require(t => typeof(IMessage).IsAssignableFrom(t));

            //get the static field Descriptor from T
            var descriptor = (MessageDescriptor)protoType
               .GetProperty("Descriptor", BindingFlags.Static | BindingFlags.Public)
               .GetValue(null);
            return ShortenedFullName(descriptor);
        }

        public static Any NoMessage = Any.Pack(null, "NoMessage");

        public static Any ToAny<T>(this T protobufObject) where T : IMessage<T>
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

        public static ByteString ToUtf8ByteString(this string utf8String)
        {
            return ByteString.CopyFromUtf8(utf8String);
        }
    }
}
