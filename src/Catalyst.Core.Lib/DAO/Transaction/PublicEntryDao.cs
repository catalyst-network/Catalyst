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
using System.ComponentModel.DataAnnotations.Schema;
using AutoMapper;
using Catalyst.Abstractions.DAO;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Core.Lib.DAO.Cryptography;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable WrongIndentSize

namespace Catalyst.Core.Lib.DAO.Transaction
{
    public class PublicEntryDao : DaoBase
    {
        public ulong Nonce { get; set; }
        public string ReceiverAddress { get; set; }
        public string SenderAddress { get; set; }
        public string Data { get; set; }
        public string Amount { get; set; }
        public DateTime TimeStamp { get; set; }
        public SignatureDao Signature { set; get; }
        public string GasPrice { get; set; }
        public ulong GasLimit { get; set; }

        [Column]

        // ReSharper disable once UnusedMember.Local
        private TransactionBroadcastDao TransactionBroadcastDao { get; set; }
    }

    public sealed class PublicEntryMapperInitialiser : IMapperInitializer
    {
        private readonly IHashProvider _hashProvider;

        public PublicEntryMapperInitialiser(IHashProvider hashProvider) { 
            _hashProvider = hashProvider; 
        }

        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.AllowNullDestinationValues = true;

            cfg.CreateMap<PublicEntry, PublicEntryDao>()
               .ForMember(d => d.Id, opt => opt.MapFrom(src => _hashProvider.ComputeMultiHash(src)))
               .ForMember(d => d.Amount,
                    opt => opt.ConvertUsing(new ByteStringToUInt256StringConverter(), s => s.Amount))
               .ForMember(e => e.Data, opt => opt.ConvertUsing<ByteStringToBase32Converter, ByteString>())
               .ForMember(d => d.ReceiverAddress,
                    opt => opt.ConvertUsing(new ByteStringToBase32Converter(), s => s.ReceiverAddress))
               .ForMember(d => d.SenderAddress,
                    opt => opt.ConvertUsing(new ByteStringToBase32Converter(), s => s.SenderAddress))
               .ForMember(d => d.Nonce, opt => opt.MapFrom(s => s.Nonce))
               .ForMember(d => d.GasPrice, opt => opt.ConvertUsing(new ByteStringToBase32Converter(), s => s.GasPrice))
               .ForMember(d => d.GasLimit, opt => opt.MapFrom(s => s.GasLimit))
               .ForMember(d => d.TimeStamp, opt => opt.MapFrom(s => s.Timestamp));

            cfg.CreateMap<PublicEntryDao, PublicEntry>()
               .ForMember(d => d.Amount,
                    opt => opt.ConvertUsing(new UInt256StringToByteStringConverter(), s => s.Amount))
               .ForMember(e => e.Data, opt => opt.ConvertUsing<Base32ToByteStringFormatter, string>())
               .ForMember(d => d.ReceiverAddress,
                    opt => opt.ConvertUsing(new Base32ToByteStringFormatter(), s => s.ReceiverAddress))
               .ForMember(d => d.SenderAddress,
                    opt => opt.ConvertUsing(new Base32ToByteStringFormatter(), s => s.SenderAddress))
               .ForMember(d => d.Nonce, opt => opt.MapFrom(s => s.Nonce))
               .ForMember(d => d.GasPrice, opt => opt.ConvertUsing(new Base32ToByteStringFormatter(), s => s.GasPrice))
               .ForMember(d => d.GasLimit, opt => opt.MapFrom(s => s.GasLimit))
               .ForMember(d => d.Timestamp, opt => opt.MapFrom(s => s.TimeStamp));

            cfg.CreateMap<DateTime, Timestamp>().ConvertUsing(s => s.ToTimestamp());
            cfg.CreateMap<Timestamp, DateTime>().ConvertUsing(s => s.ToDateTime());
        }
    }
}
