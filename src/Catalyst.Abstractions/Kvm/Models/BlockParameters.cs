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
using Nethermind.Core.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public sealed class BlockParameter
    {
        private static readonly BlockParameter Earliest = new BlockParameter(BlockParameterType.Earliest);

        private static readonly BlockParameter Pending = new BlockParameter(BlockParameterType.Pending);

        private static readonly BlockParameter Latest = new BlockParameter(BlockParameterType.Latest);

        public BlockParameterType Type { get; set; }
        public long? BlockNumber { get; set; }

        public BlockParameter() { }

        public BlockParameter(BlockParameterType type)
        {
            Type = type;
            BlockNumber = null;
        }

        public void FromJson(string jsonValue)
        {
            switch (jsonValue)
            {
                case string earliest when string.Equals(earliest, "earliest", StringComparison.InvariantCultureIgnoreCase):
                    Type = BlockParameterType.Earliest;
                    return;
                case string pending when string.Equals(pending, "pending", StringComparison.InvariantCultureIgnoreCase):
                    Type = BlockParameterType.Pending;
                    return;
                case string latest when string.Equals(latest, "latest", StringComparison.InvariantCultureIgnoreCase):
                    Type = BlockParameterType.Latest;
                    return;
                case string empty when string.IsNullOrWhiteSpace(empty):
                    Type = BlockParameterType.Latest;
                    return;
                case null:
                    Type = BlockParameterType.Latest;
                    return;
                default:
                    Type = BlockParameterType.BlockNumber;
                    BlockNumber = LongConverter.FromString(jsonValue.Trim('"'));
                    return;
            }
        }

        public override string ToString()
        {
            return $"{Type}, {BlockNumber}";
        }

        public FilterBlock ToFilterBlock() =>
            BlockNumber != null
                ? new FilterBlock(BlockNumber ?? 0)
                : new FilterBlock(Type.ToFilterBlockType());
    }
}
