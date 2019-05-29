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
using System.Security.Cryptography;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Util;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Transaction;
using Dawn;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Type = System.Type;

namespace Catalyst.Common.Extensions
{
    public static class ProtobufExtensions
    {
        private const string CatalystProtocol = "Catalyst.Protocol";
        private static readonly string RequestSuffix = "Request";
        private static readonly string ResponseSuffix = "Response";
        private static readonly string BroadcastSuffix = "Broadcast";

        private static readonly Dictionary<string, string> ProtoToClrNameMapper;
        private static readonly List<string> ProtoGossipAllowedMessages;

        static ProtobufExtensions()
        {
            ProtoToClrNameMapper = typeof(AnySigned).Assembly.ExportedTypes
               .Where(t => typeof(IMessage).IsAssignableFrom(t))
               .Select(t => ((IMessage) Activator.CreateInstance(t)).Descriptor)
               .ToDictionary(d => d.ShortenedFullName(), d => d.ClrType.FullName);
            ProtoGossipAllowedMessages = ProtoToClrNameMapper.Keys
               .Where(t => t.EndsWith(BroadcastSuffix))
               .ToList();
        }

        public static string ShortenedFullName(this MessageDescriptor descriptor)
        {
            //we don't need to serialise the complete full name of our own types
            return descriptor.FullName.Remove(0, CatalystProtocol.Length + 1);
        }

        public static string ShortenedProtoFullName(this Type protoType)
        {
            Guard.Argument(protoType, nameof(protoType)).Require(t => typeof(IMessage).IsAssignableFrom(t));
            
            //get the static field Descriptor from T
            var descriptor = (MessageDescriptor) protoType
               .GetProperty("Descriptor", BindingFlags.Static | BindingFlags.Public)
               .GetValue(null);
            return ShortenedFullName(descriptor);
        }

        public static AnySigned ToAnySigned(this IMessage protobufObject,
            PeerId senderId,
            Guid correlationId = default)
        {
            var typeUrl = protobufObject.Descriptor.ShortenedFullName();
            Guard.Argument(senderId, nameof(senderId)).NotNull();
            Guard.Argument(correlationId, nameof(correlationId))
               .Require(c => !typeUrl.EndsWith(ResponseSuffix) || c != default,
                    g => $"{typeUrl} is a response type and needs a correlationId");

            var anySigned = new AnySigned
            {
                PeerId = senderId,
                CorrelationId = (correlationId == default ? Guid.NewGuid() : correlationId).ToByteString(),

                //todo: sign the `correlationId` and `value` bytes with publicKey instead
                Signature = senderId.PublicKey,
                TypeUrl = typeUrl,
                Value = protobufObject.ToByteString()
            };
            return anySigned;
        }

        public static bool CheckIfMessageIsGossip(this AnySigned message)
        {
            return message.TypeUrl.EndsWith(nameof(AnySigned)) &&
                ProtoGossipAllowedMessages.Contains(AnySigned.Parser.ParseFrom(message.Value).TypeUrl);
        }

        public static T FromAnySigned<T>(this AnySigned message) where T : IMessage<T>
        {
            //todo check the message signature with the PeerId.PublicKey and value fields
            if (message.PeerId.PublicKey != message.Signature)
                throw new CryptographicException("Signature of the message doesn't match with sender's public Key");
            var empty = (T) Activator.CreateInstance(typeof(T));
            var typed = (T) empty.Descriptor.Parser.ParseFrom(message.Value);
            return typed;
        }

        public static ByteString ToUtf8ByteString(this string utf8String)
        {
            return ByteString.CopyFromUtf8(utf8String);
        }

        public static Guid ToGuid(this ByteString guidBytes)
        {
            return new Guid(guidBytes.ToByteArray());
        }

        public static ByteString ToByteString(this Guid guid)
        {
            return guid.ToByteArray().ToByteString();
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
