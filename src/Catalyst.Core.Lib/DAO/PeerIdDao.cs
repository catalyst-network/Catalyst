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
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Protocol.Peer;
using Google.Protobuf;

namespace Catalyst.Core.Lib.DAO
{
    public class PeerIdDao : DaoBase
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string PublicKey { get; set; }
    }

    public class PeerIdMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<PeerId, PeerIdDao>()
               .ForMember(e => e.PublicKey,
                    opt => opt.ConvertUsing<ByteStringToStringPubKeyConverter, ByteString>())
               .ForMember(e => e.Ip,
                    opt => opt.ConvertUsing<ByteStringToIpAddressConverter, ByteString>());

            cfg.CreateMap<PeerIdDao, PeerId>()
               .ForMember(e => e.PublicKey,
                    opt => opt.ConvertUsing<StringKeyUtilsToByteStringFormatter, string>())
               .ForMember(e => e.Ip,
                    opt => opt.ConvertUsing<IpAddressToByteStringConverter, string>());
        }
    }
}
