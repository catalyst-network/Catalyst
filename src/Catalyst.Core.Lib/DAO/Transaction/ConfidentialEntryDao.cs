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

using System.ComponentModel.DataAnnotations.Schema;
using AutoMapper;
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Catalyst.Core.Lib.DAO.Transaction
{
    public class ConfidentialEntryDao : DaoBase
    {
        public ulong Nonce { get; set; }
        public string ReceiverPublicKey { get; set; }
        public string SenderPublicKey { get; set; }
        public string TransactionFees { get; set; }
        public string PedersenCommitment { get; set; }
        public string RangeProof { get; set; }

        [Column]

        // ReSharper disable once UnusedMember.Local
        private TransactionBroadcastDao TransactionBroadcastDao { get; set; }
    }

    public sealed class ConfidentialEntryMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ConfidentialEntry, ConfidentialEntryDao>()
               .ForMember(e => e.RangeProof,
                    opt => opt.MapFrom(x => x.RangeProof.ToByteString().ToBase64()))
               .ForMember(e => e.PedersenCommitment,
                    opt => opt.ConvertUsing<ByteStringToStringBase64Converter, ByteString>())
               .ForMember(d => d.ReceiverPublicKey,
                    opt => opt.ConvertUsing(new ByteStringToBase32Converter(), s => s.ReceiverPublicKey))
               .ForMember(d => d.SenderPublicKey,
                    opt => opt.ConvertUsing(new ByteStringToBase32Converter(), s => s.SenderPublicKey))
               .ForMember(d => d.TransactionFees,
                    opt => opt.ConvertUsing(new ByteStringToUInt256StringConverter(), s => s.TransactionFees));

            cfg.CreateMap<ConfidentialEntryDao, ConfidentialEntry>()
               .ForMember(e => e.RangeProof,
                    opt => opt.MapFrom(x => RangeProof.Parser.ParseFrom(ByteString.FromBase64(x.RangeProof))))
               .ForMember(e => e.PedersenCommitment,
                    opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>())
               .ForMember(d => d.ReceiverPublicKey,
                    opt => opt.ConvertUsing(new Base32ToByteStringFormatter(), s => s.ReceiverPublicKey))
               .ForMember(d => d.SenderPublicKey,
                    opt => opt.ConvertUsing(new Base32ToByteStringFormatter(), s => s.SenderPublicKey))
               .ForMember(d => d.TransactionFees,
                    opt => opt.ConvertUsing(new UInt256StringToByteStringConverter(), s => s.TransactionFees));
        }
    }
}
