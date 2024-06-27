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
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO.Peer;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Wire;
using Nethermind.Core;

namespace Catalyst.Core.Lib.DAO.Deltas
{
    public class FavouriteDeltaBroadcastDao : DaoBase
    {
        public CandidateDeltaBroadcastDao Candidate { get; set; }
        public string Voter { get; set; }
    }

    public class FavouriteDeltaBroadcastMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<FavouriteDeltaBroadcast, FavouriteDeltaBroadcastDao>()
                .ForMember(e => e.Voter, opt => opt.MapFrom(x => new Address(x.Voter.ToByteArray())));

            cfg.CreateMap<FavouriteDeltaBroadcastDao, FavouriteDeltaBroadcast>()
               .ForMember(e => e.Voter, opt => opt.MapFrom(x => new Address(x.Voter).Bytes.ToByteString()));
        }
    }
}
