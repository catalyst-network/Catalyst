#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Type = System.Type;

namespace Catalyst.Protocol
{
    public static class ProtobufExtensions
    {
        private const string CatalystProtocol = "Catalyst.Protocol";

        public static readonly string BroadcastSuffix = "Broadcast";
        public static readonly string RequestSuffix = "Request";
        public static readonly string ResponseSuffix = "Response";

        private static readonly Dictionary<string, string> ProtoToClrNameMapper = 
            typeof(ProtocolMessage).Assembly.ExportedTypes
                .Where(t => typeof(IMessage).IsAssignableFrom(t))
                .Select(t => ((IMessage) Activator.CreateInstance(t)).Descriptor)
                .ToDictionary(d => d.ShortenedFullName(), d => d.ClrType.FullName);

        private static readonly List<string> ProtoBroadcastAllowedMessages = 
            ProtoToClrNameMapper.Keys.Where(t => t.EndsWith(BroadcastSuffix)).ToList();

        private static readonly List<string> ProtoRequestAllowedMessages = 
            ProtoToClrNameMapper.Keys.Where(t => t.EndsWith(RequestSuffix)).ToList();

        private static readonly List<string> ProtoResponseAllowedMessages =
            ProtoToClrNameMapper.Keys.Where(t => t.EndsWith(ResponseSuffix)).ToList();

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
        
        public static bool IsRequestType(this Type type)
        {
            var shortType = ShortenedProtoFullName(type);
            return ProtoRequestAllowedMessages.Contains(shortType);
        }

        public static bool IsResponseType(this Type type)
        {
            var shortType = ShortenedProtoFullName(type);
            return ProtoResponseAllowedMessages.Contains(shortType);
        }

        public static bool IsBroadcastType(this Type type)
        {
            var shortType = ShortenedProtoFullName(type);
            return ProtoBroadcastAllowedMessages.Contains(shortType);
        }

        public static bool IsBroadCastMessage(this ProtocolMessage message)
        {
            return message.TypeUrl.EndsWith(nameof(ProtocolMessageSigned)) &&
                ProtoBroadcastAllowedMessages.Contains(ProtocolMessageSigned.Parser.ParseFrom(message.Value).Message.TypeUrl);
        }

        public static T FromProtocolMessage<T>(this ProtocolMessage message) where T : IMessage<T>
        {
            var empty = (T)Activator.CreateInstance(typeof(T));
            var typed = (T)empty.Descriptor.Parser.ParseFrom(message.Value);
            return typed;
        }

        public static ByteString ToUtf8ByteString(this string utf8String)
        {
            return ByteString.CopyFromUtf8(utf8String);
        }

        public static string ToJsonString(this IMessage message)
        {
            return JsonFormatter.Default.Format(message);
        }

        public static string GetRequestType(this string responseTypeUrl)
        {
            return SwapSuffixes(responseTypeUrl, ResponseSuffix, RequestSuffix);
        }

        public static string GetResponseType(this string requestTypeUrl)
        {
            return SwapSuffixes(requestTypeUrl, RequestSuffix, ResponseSuffix);
        }

        private static string SwapSuffixes(string requestTypeUrl, string originalSuffix, string targetSuffix)
        {
            Guard.Argument(requestTypeUrl, nameof(requestTypeUrl)).NotNull()
               .Require(t => t.EndsWith(originalSuffix), t => $"{t} should end with {originalSuffix}");
            return requestTypeUrl
                   .Remove(requestTypeUrl.Length - originalSuffix.Length, originalSuffix.Length)
              + targetSuffix;
        }
    }
}
