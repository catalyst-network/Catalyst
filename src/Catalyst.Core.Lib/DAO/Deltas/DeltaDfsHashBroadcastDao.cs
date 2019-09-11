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
using Catalyst.Core.Lib.Converters;
using Catalyst.Protocol.Deltas;

namespace Catalyst.Core.Lib.DAO.Deltas
{
    public class DeltaDfsHashBroadcastDao : DaoBase<DeltaDfsHashBroadcast, DeltaDfsHashBroadcastDao>
    {
        public string DeltaDfsHash { get; set; }
        public string PreviousDeltaDfsHash { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<DeltaDfsHashBroadcast, DeltaDfsHashBroadcastDao>().ReverseMap();

            cfg.CreateMap<DeltaDfsHashBroadcast, DeltaDfsHashBroadcastDao>()
               .ForMember(d => d.DeltaDfsHash, opt => opt.ConvertUsing(new ByteStringToStringBase64Formatter(), s => s.DeltaDfsHash));
            cfg.CreateMap<DeltaDfsHashBroadcastDao, DeltaDfsHashBroadcast>()
               .ForMember(d => d.DeltaDfsHash, opt => opt.ConvertUsing(new StringBase64ToByteStringFormatter(), s => s.DeltaDfsHash));

            cfg.CreateMap<DeltaDfsHashBroadcast, DeltaDfsHashBroadcastDao>()
               .ForMember(d => d.PreviousDeltaDfsHash, opt => opt.ConvertUsing(new ByteStringToStringBase64Formatter(), s => s.PreviousDeltaDfsHash));
            cfg.CreateMap<DeltaDfsHashBroadcastDao, DeltaDfsHashBroadcast>()
               .ForMember(d => d.PreviousDeltaDfsHash, opt => opt.ConvertUsing(new StringBase64ToByteStringFormatter(), s => s.PreviousDeltaDfsHash));
        }
    }
}
