using System;
using Nethermind.Core.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class BlockParameter
    {
        public static BlockParameter Earliest = new BlockParameter(BlockParameterType.Earliest);

        public static BlockParameter Pending = new BlockParameter(BlockParameterType.Pending);

        public static BlockParameter Latest = new BlockParameter(BlockParameterType.Latest);

        public BlockParameterType Type { get; set; }
        public long? BlockNumber { get; set; }

        public BlockParameter()
        {
        }

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

        public FilterBlock ToFilterBlock()
            => BlockNumber != null
                ? new FilterBlock(BlockNumber ?? 0)
                : new FilterBlock(Type.ToFilterBlockType());
    }
}
