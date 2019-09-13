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
using Google.Protobuf;

namespace Catalyst.Core.Lib.DAO
{
    public class PeerIdDao : DaoBase<PeerId, PeerIdDao>
    {
        public string ClientId { get; set; }
        public string ProtocolVersion { get; set; }
        public string Ip { get; set; }
        public ushort Port { get; set; }
        public string PublicKey { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<PeerId, PeerIdDao>()
               .ForMember(e => e.PublicKey,
                    opt => opt.ConvertUsing<ByteStringToStringPubKeyConverter, ByteString>())
               .ForMember(d => d.Port, 
                    opt => opt.ConvertUsing<ByteStringToUShortFormatter, ByteString>())
               .ForMember(e => e.ClientId,
                    opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.ProtocolVersion,
                    opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.Ip,
                    opt => opt.ConvertUsing<ByteStringToIpAddressConverter, ByteString>());

            cfg.CreateMap<PeerIdDao, PeerId>()
               .ForMember(e => e.PublicKey,
                    opt => opt.ConvertUsing<StringKeyUtilsToByteStringFormatter, string>())
               .ForMember(d => d.Port, 
                    opt => opt.ConvertUsing<UShortToByteStringFormatter, ushort>())
               .ForMember(e => e.ClientId,
                    opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.ProtocolVersion,
                    opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.Ip,
                    opt => opt.ConvertUsing<IpAddressToByteStringConverter, string>());
        }
    }
}
