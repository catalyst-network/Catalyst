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

namespace Catalyst.Core.Lib.DAO
{
    public class ConfidentialEntryDao : DaoBase<ConfidentialEntry, ConfidentialEntryDao>
    {
        public BaseEntryDao Base { get; set; }
        public string PedersenCommitment { get; set; }
        public RangeProofDao RangeProof { get; set; }
        private TransactionBroadcastDao TransactionBroadcastDao { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ConfidentialEntry, ConfidentialEntryDao>()
               .ForMember(e => e.PedersenCommitment,
                    opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>());

            cfg.CreateMap<ConfidentialEntryDao, ConfidentialEntry>()
               .ForMember(e => e.PedersenCommitment,
                    opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>());
        }
    }
}
