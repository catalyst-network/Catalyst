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
using Google.Protobuf;

namespace Catalyst.Protocol.DAO.Deltas
{
    public class FavouriteDeltaBroadcastDao : DaoBase
    {
        public CandidateDeltaBroadcastDao Candidate { get; set; }
        public PeerIdDao VoterId { get; set; }

        public FavouriteDeltaBroadcastDao()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<FavouriteDeltaBroadcast, FavouriteDeltaBroadcastDao>().ReverseMap();
                cfg.CreateMap<CandidateDeltaBroadcast, CandidateDeltaBroadcastDao>().ReverseMap();

                cfg.CreateMap<PeerId, PeerIdDao>().ReverseMap();

                cfg.CreateMap<PeerId, PeerIdDao>()
                   .ForMember(d => d.Port, opt => opt.ConvertUsing(new ByteStringToUShortFormatter(), s => s.Port));

                cfg.CreateMap<PeerIdDao, PeerId>()
                   .ForMember(d => d.Port, opt => opt.ConvertUsing(new UShortToByteStringFormatter(), s => s.Port));

                cfg.CreateMap<ByteString, string>().ConvertUsing(s => s.ToBase64());
                cfg.CreateMap<string, ByteString>().ConvertUsing(s => ByteString.FromBase64(s));
            });

            Mapper = config.CreateMapper();
        }

        public override IMessage ToProtoBuff()
        {
            return (IMessage) Mapper.Map<FavouriteDeltaBroadcast>(this);
        }

        public override DaoBase ToDao(IMessage protoBuff)
        {
            return Mapper.Map<FavouriteDeltaBroadcastDao>((FavouriteDeltaBroadcast) protoBuff);
        }
    }
}
