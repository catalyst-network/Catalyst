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
using System.Collections.Generic;
using AutoMapper;
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Catalyst.Core.Lib.DAO
{
    public class EntryRangeProofDao : DaoBase<EntryRangeProof, EntryRangeProofDao>
    {
        public IEnumerable<string> V { get; set; }
        public string A { get; set; }
        public string S { get; set; }
        public string T1 { get; set; }
        public string T2 { get; set; }
        public string Tau { get; set; }
        public string Mu { get; set; }
        public List<string> L { get; set; }
        public List<string> R { get; set; }
        public string APrime0 { get; set; }
        public string BPrime0 { get; set; }
        public string T { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<EntryRangeProof, EntryRangeProofDao>()
               .ForMember(e => e.V, opt => opt.ConvertUsing<RepeatedFieldToListConverter<ByteString, string, ByteStringToStringBase64Converter>, RepeatedField<ByteString>>())
               .ForMember(e => e.L, opt => opt.ConvertUsing<RepeatedFieldToListConverter<ByteString, string, ByteStringToStringBase64Converter>, RepeatedField<ByteString>>())
               .ForMember(e => e.R, opt => opt.ConvertUsing<RepeatedFieldToListConverter<ByteString, string, ByteStringToStringBase64Converter>, RepeatedField<ByteString>>())
               .ForMember(e => e.Id, opt => opt.Ignore())
               .ForAllOtherMembers(opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>());

            cfg.CreateMap<EntryRangeProofDao, EntryRangeProof>()
               .ForMember(e => e.V, opt => opt.ConvertUsing<ListToRepeatedFieldConverter<string, ByteString, StringBase64ToByteStringConverter>, List<string>>())
               .ForMember(e => e.L, opt => opt.ConvertUsing<ListToRepeatedFieldConverter<string, ByteString, StringBase64ToByteStringConverter>, List<string>>())
               .ForMember(e => e.R, opt => opt.ConvertUsing<ListToRepeatedFieldConverter<string, ByteString, StringBase64ToByteStringConverter>, List<string>>())
               .ForAllOtherMembers(opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>());

            cfg.AllowNullCollections = true;
        }
    }
}
