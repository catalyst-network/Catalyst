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
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Google.Protobuf;

namespace Catalyst.Core.Lib.DAO.Deltas
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

            cfg.CreateMap<CandidateDeltaBroadcast, CandidateDeltaBroadcastDao>()
               .ForMember(e => e.Hash,
                    opt => opt.ConvertUsing<ByteStringToDfsHashConverter, ByteString>());
            cfg.CreateMap<CandidateDeltaBroadcastDao, CandidateDeltaBroadcast>()
               .ForMember(e => e.Hash,
                    opt => opt.ConvertUsing<DfsHashToByteStringConverter, string>());

            cfg.CreateMap<CandidateDeltaBroadcast, CandidateDeltaBroadcastDao>()
               .ForMember(e => e.PreviousDeltaDfsHash,
                    opt => opt.ConvertUsing<ByteStringToDfsHashConverter, ByteString>());
            cfg.CreateMap<CandidateDeltaBroadcastDao, CandidateDeltaBroadcast>()
               .ForMember(e => e.PreviousDeltaDfsHash,
                    opt => opt.ConvertUsing<DfsHashToByteStringConverter, string>());
        }
    }
}
