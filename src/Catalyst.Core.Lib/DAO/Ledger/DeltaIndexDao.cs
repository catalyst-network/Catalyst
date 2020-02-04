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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using LibP2P;
using TheDotNetLeague.MultiFormats.MultiBase;

namespace Catalyst.Core.Lib.DAO.Ledger
{
    public class DeltaIndexDao : DaoBase
    {
        public int Height { set; get; }
        public string Cid { set; get; }
    }

    public class DeltaIndexMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<DeltaIndex, DeltaIndexDao>().ReverseMap();

            cfg.CreateMap<DeltaIndex, DeltaIndexDao>()
               .ForMember(a => a.Id, opt => opt.MapFrom(x => x.Height))
               .ForMember(a => a.Height, opt => opt.UseDestinationValue())
               .ForMember(a => a.Cid,
                    opt => opt.MapFrom(x => Cid.Decode(MultiBase.Encode(x.Cid.ToByteArray(), "base32"))));

            cfg.CreateMap<DeltaIndexDao, DeltaIndex>()
               .ForMember(a => a.Height, opt => opt.UseDestinationValue())
               .ForMember(a => a.Cid,
                    opt => opt.MapFrom(x => x.Cid.FromBase32().ToByteString()));
        }
    }
}
