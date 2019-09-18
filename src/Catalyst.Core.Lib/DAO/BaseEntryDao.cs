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

namespace Catalyst.Core.Lib.DAO
{
    public class BaseEntryDao : DaoBase<BaseEntry, BaseEntryDao>
    {
        public string ReceiverPublicKey { get; set; }
        public string SenderPublicKey { get; set; }
        public ulong TransactionFees { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<BaseEntry, BaseEntryDao>()
                .ForMember(d => d.ReceiverPublicKey,
                    opt => opt.ConvertUsing(new ByteStringToStringPubKeyConverter(), s => s.ReceiverPublicKey))
                .ForMember(d => d.SenderPublicKey,
                    opt => opt.ConvertUsing(new ByteStringToStringPubKeyConverter(), s => s.SenderPublicKey))
                .ForMember(d => d.TransactionFees,
                    opt => opt.ConvertUsing(new ByteStringToUInt256Converter(), s => s.TransactionFees));

            cfg.CreateMap<BaseEntryDao, BaseEntry>()
                .ForMember(d => d.ReceiverPublicKey,
                    opt => opt.ConvertUsing(new StringKeyUtilsToByteStringFormatter(), s => s.ReceiverPublicKey))
                .ForMember(d => d.SenderPublicKey,
                    opt => opt.ConvertUsing(new StringKeyUtilsToByteStringFormatter(), s => s.SenderPublicKey))
                .ForMember(d => d.TransactionFees,
                    opt => opt.ConvertUsing(new UInt256ToByteStringConverter(), s => s.TransactionFees));
        }
    }
}
