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
using Catalyst.Protocol.Cryptography;

namespace Catalyst.Core.Lib.DAO
{
    public class SignatureDao : DaoBase<Signature, SignatureDao>
    {
        public SigningContextDao SigningContext { get; set; }
        public string RawBytes { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Signature, SignatureDao>()
               .ForMember(d => d.RawBytes, 
                    opt => opt.ConvertUsing(new ByteStringToStringPubKeyConverter(), s => s.RawBytes));

            cfg.CreateMap<SignatureDao, Signature>()
               .ForMember(d => d.RawBytes,
                    opt => opt.ConvertUsing(new StringKeyUtilsToByteStringFormatter(), s => s.RawBytes));
        }
    }
}
