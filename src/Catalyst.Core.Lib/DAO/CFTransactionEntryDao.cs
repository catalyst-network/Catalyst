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
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;

namespace Catalyst.Core.Lib.DAO
{
    public class CfTransactionEntryDao : DaoBase<CFTransactionEntry, CfTransactionEntryDao>
    {
        public string PubKey { get; set; }
        public string PedersenCommit { get; set; }
        public EntryRangeProofDao EntryRangeProofs { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<EntryRangeProof, EntryRangeProofDao>().ReverseMap();
            cfg.CreateMap<CFTransactionEntry, CfTransactionEntryDao>()
               .ForMember(e => e.PedersenCommit,
                    opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(e => e.PubKey,
                    opt => opt.ConvertUsing<ByteStringToStringPubKeyConverter, ByteString>());

            cfg.CreateMap<CfTransactionEntryDao, CFTransactionEntry>()
               .ForMember(e => e.PedersenCommit,
                    opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(e => e.PubKey,
                    opt => opt.ConvertUsing<StringKeyUtilsToByteStringFormatter, string>());
        }
    }
}
