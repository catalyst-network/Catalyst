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
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Converters;
using Catalyst.Protocol.Deltas;

namespace Catalyst.Protocol.DAO.Deltas
{
    public class CandidateDeltaBroadcastDao : DaoBase<CandidateDeltaBroadcast, CandidateDeltaBroadcastDao>
    {
        public string Hash { get; set; }
        public PeerIdDao ProducerId { get; set; }
        public string PreviousDeltaDfsHash { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<CandidateDeltaBroadcast, CandidateDeltaBroadcastDao>().ReverseMap();
            cfg.CreateMap<PeerId, PeerIdDao>().ReverseMap();

            cfg.CreateMap<PeerId, PeerIdDao>()
               .ForMember(d => d.Port, opt => opt.ConvertUsing(new ByteStringToUShortFormatter(), s => s.Port));

            cfg.CreateMap<PeerIdDao, PeerId>()
               .ForMember(d => d.Port, opt => opt.ConvertUsing(new UShortToByteStringFormatter(), s => s.Port));

            cfg.CreateMap<CandidateDeltaBroadcast, CandidateDeltaBroadcastDao>()
               .ForMember(d => d.PreviousDeltaDfsHash, opt => opt.ConvertUsing(new ByteStringToStringBase64Formatter(), s => s.PreviousDeltaDfsHash));
            cfg.CreateMap<CandidateDeltaBroadcastDao, CandidateDeltaBroadcast>()
               .ForMember(d => d.PreviousDeltaDfsHash, opt => opt.ConvertUsing(new StringBase64ToByteStringFormatter(), s => s.PreviousDeltaDfsHash));
        }
    }
}
