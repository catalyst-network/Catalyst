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

//
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Catalyst.Core.Modules.Cryptography.BulletProofs.Interfaces;
// using Dawn;
//
// namespace Catalyst.Protocol.Common
// {
//     //https://github.com/catalyst-network/protobuffs-protocol-sdk-csharp/issues/41
//     //move that to the Utils project after Dennis' project gets created.
//
//     /// <inheritdoc />
//     public class Address : IAddress
//     {
//         public static readonly int ByteLength = 20;
//
//         private readonly byte[] _nonPrefixedContent;
//         private byte[] _rawBytes;
//
//         public Address(IPublicKey publicKey,
//             Network network, 
//             IMultihashAlgorithm hashingAlgorithm, 
//             bool isSmartContract)
//         {
//             Guard.Argument(publicKey, nameof(publicKey)).NotNull();
//             Network = network;
//             IsSmartContract = isSmartContract;
//
//             _nonPrefixedContent = publicKey.Bytes
//                .ComputeRawHash(hashingAlgorithm)
//                .TakeLast(ByteLength - 2)
//                .ToArray();
//         }
//
//         public Address(IList<byte> rawBytes)
//         {
//             Guard.Argument(rawBytes, nameof(rawBytes)).NotNull()
//                .Require(b => b.Count == ByteLength,
//                     b => $"{nameof(rawBytes)} is {rawBytes.Count} long but should be {ByteLength} instead.")
//                .Require(b => Enum.IsDefined(typeof(Network), (int) b[0]),
//                     b => $"Invalid byte at position 0, byte does not map to a known Network.")
//                .Require(b => b[1] == 0 || b[1] == 1,
//                     b => $"Invalid byte at position 1, byte should be either 0 or 1 but was {b[1]}.");
//
//             Network = (Network) rawBytes[0];
//
//             IsSmartContract = rawBytes[1] == 1;
//             _nonPrefixedContent = rawBytes.TakeLast(ByteLength - 2).ToArray();
//         }
//
//         /// <inheritdoc />
//         public Network Network { get; }
//
//         /// <inheritdoc />
//         public bool IsSmartContract { get; }
//
//         /// <inheritdoc />
//         public byte[] RawBytes =>
//             _rawBytes ?? (_rawBytes = new[]
//                 {
//                     (byte) Network,
//                     (byte) (IsSmartContract ? 1 : 0),
//                 }
//                .Concat(_nonPrefixedContent)
//                .ToArray());
//
//         /// <inheritdoc />
//         public string AsBase32Crockford => RawBytes.Take(20).AsBase32Address();
//     }
// }
