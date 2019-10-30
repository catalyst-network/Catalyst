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
using AutoMapper;
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.Repository.Attributes;
using Catalyst.Core.Lib.Util;

namespace Catalyst.Core.Lib.DAO
{
    [Audit]
    public sealed class PeerDao : DaoBase
    {
        public PeerIdDao PeerIdentifier { get; set; }

        public int Reputation { get; set; }

        public bool BlackListed { get; set; }

        /// <summary>
        ///     When peer was first seen by the peer.
        /// </summary>
        public DateTime Created { get; set; }

        public DateTime? Modified { get; set; }

        public DateTime LastSeen { get; set; }

        public bool IsAwolPeer => InactiveFor > TimeSpan.FromMinutes(30);

        public TimeSpan InactiveFor => DateTimeUtil.UtcNow - LastSeen;

        public void Touch() { LastSeen = DateTimeUtil.UtcNow; }
    }

    public class PeerDaoMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Peer, PeerDao>();
            cfg.AllowNullDestinationValues = true;

            cfg.CreateMap<PeerDao, Peer>()
               .ForMember(e => e.Reputation, opt => opt.UseDestinationValue())
               .ForMember(e => e.BlackListed, opt => opt.UseDestinationValue())
               .ForMember(e => e.Created, opt => opt.UseDestinationValue())
               .ForMember(e => e.Modified, opt => opt.UseDestinationValue())
               .ForMember(e => e.LastSeen, opt => opt.UseDestinationValue())
               .ForMember(e => e.PeerId, opt => opt.UseDestinationValue());
        }
    }
}
