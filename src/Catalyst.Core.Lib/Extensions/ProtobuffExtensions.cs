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
using System.Net;
using System.Reflection;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Network;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Dawn;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Type = System.Type;

namespace Catalyst.Core.Lib.Extensions
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
               .Select(t => ((IMessage)Activator.CreateInstance(t)).Descriptor)
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
            return message.TypeUrl.EndsWith(nameof(ProtocolMessage)) &&
                ProtoBroadcastAllowedMessages.Contains(ProtocolMessage.Parser.ParseFrom(message.Value).TypeUrl);
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

        public static ProtocolMessage ToProtocolMessage(this IMessage protobufObject,
            PeerId senderId,
            ICorrelationId correlationId = default)
        {
            var typeUrl = protobufObject.Descriptor.ShortenedFullName();
            Guard.Argument(senderId, nameof(senderId)).NotNull();

            if (typeUrl.EndsWith(MessageTypes.Response.Name))
            {
                Guard.Argument(correlationId, nameof(correlationId)).NotNull();
            }

            return new ProtocolMessage
            {
                PeerId = senderId,
                CorrelationId = (correlationId?.Id ?? CorrelationId.GenerateCorrelationId().Id).ToByteString(),
                TypeUrl = typeUrl,
                Value = protobufObject.ToByteString()
            };
        }

        public static ICorrelationId ToCorrelationId(this ByteString guidBytes)
        {
            var bytes = guidBytes?.ToByteArray();

            var validBytes = bytes?.Length == CorrelationId.GuidByteLength
                ? bytes
                : (bytes ?? new byte[0]).Concat(Enumerable.Repeat((byte)0, CorrelationId.GuidByteLength))
               .Take(CorrelationId.GuidByteLength).ToArray();

            return new CorrelationId(new Guid(validBytes));
        }

        public static ByteString IpAddressToProtobuf(this IPAddress ipAddress)
        {
            return ByteString.CopyFrom(ipAddress.To16Bytes());
        }

        public static void GenerateId(this PublicEntry publicEntry, IHashProvider hashProvider)
        {
            publicEntry.Id = hashProvider.ComputeMultiHash(publicEntry.ToByteArray()).ToArray();
        }
    }
}
