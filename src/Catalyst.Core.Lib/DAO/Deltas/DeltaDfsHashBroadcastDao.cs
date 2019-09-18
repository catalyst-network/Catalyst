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

using AutoMapper;
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Protocol.Wire;
using Google.Protobuf;

namespace Catalyst.Core.Lib.DAO.Deltas
{
    public class DeltaDfsHashBroadcastDao : DaoBase<DeltaDfsHashBroadcast, DeltaDfsHashBroadcastDao>
    {
        public string DeltaDfsHash { get; set; }
        public string PreviousDeltaDfsHash { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<DeltaDfsHashBroadcast, DeltaDfsHashBroadcastDao>()
               .ForMember(e => e.DeltaDfsHash,
                    opt => opt.ConvertUsing<ByteStringToDfsHashConverter, ByteString>())
               .ForMember(e => e.PreviousDeltaDfsHash,
                    opt => opt.ConvertUsing<ByteStringToDfsHashConverter, ByteString>());

            cfg.CreateMap<DeltaDfsHashBroadcastDao, DeltaDfsHashBroadcast>()
               .ForMember(e => e.DeltaDfsHash,
                    opt => opt.ConvertUsing<DfsHashToByteStringConverter, string>())
               .ForMember(e => e.PreviousDeltaDfsHash,
                    opt => opt.ConvertUsing<DfsHashToByteStringConverter, string>());
        }
    }
}
