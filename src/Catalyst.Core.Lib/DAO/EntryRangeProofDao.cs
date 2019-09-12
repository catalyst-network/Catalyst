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
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Catalyst.Core.Lib.DAO
{
    public class EntryRangeProofDao : DaoBase<EntryRangeProof, EntryRangeProofDao>
    {
        public RepeatedField<string> V { get; set; }
        public string A { get; set; }
        public string S { get; set; }
        public string T1 { get; set; }
        public string T2 { get; set; }
        public string Tau { get; set; }
        public string Mu { get; set; }
        public RepeatedField<string> L { get; set; }
        public RepeatedField<string> R { get; set; }
        public string APrime0 { get; set; }
        public string BPrime0 { get; set; }
        public string T { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            //cfg.CreateMap<EntryRangeProof, EntryRangeProofDao>().ReverseMap();

            cfg.CreateMap<EntryRangeProof, EntryRangeProofDao>()
               .ForMember(e => e.A, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.S, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.T1, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.T, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.T2, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.Tau, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.Mu, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.APrime0, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.BPrime0, opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>());

            cfg.CreateMap<EntryRangeProofDao, EntryRangeProof>()
               .ForMember(e => e.A, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.S, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.T, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.T1, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.T2, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.TAU, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.MU, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.APrime0, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.BPrime0, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>());

            cfg.CreateMap(typeof(RepeatedField<>), typeof(RepeatedField<>))
               .ConstructUsingServiceLocator();
        }
    }
}
