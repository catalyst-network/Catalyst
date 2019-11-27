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
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Core.Lib.DAO.Cryptography;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TheDotNetLeague.MultiFormats.MultiBase;

namespace Catalyst.Core.Lib.DAO.Transaction
{
    public class PublicEntryDao : DaoBase
    {
        public SignatureDao Signature { set; get; }
        public BaseEntryDao Base { get; set; }
        public string Data { get; set; }
        public string Amount { get; set; }
        public DateTime TimeStamp { get; set; }

        [Column]

        // ReSharper disable once UnusedMember.Local
        private TransactionBroadcastDao TransactionBroadcastDao { get; set; }
    }

    public sealed class PublicEntryMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.AllowNullDestinationValues = true;

            cfg.CreateMap<PublicEntry, PublicEntryDao>()
               .ForMember(d => d.Id, opt => opt.MapFrom(src => src.Id.ToBase32()))
               .ForMember(d => d.Amount, opt => opt.ConvertUsing(new ByteStringToUInt256StringConverter(), s => s.Amount))
               .ForMember(e => e.Data, opt => opt.ConvertUsing<ByteStringToBase32Converter, ByteString>());

            cfg.CreateMap<PublicEntryDao, PublicEntry>()
               .ForMember(d => d.Id, opt => opt.MapFrom(src => src.Id.FromBase32()))
               .ForMember(d => d.Amount, opt => opt.ConvertUsing(new UInt256StringToByteStringConverter(), s => s.Amount))
               .ForMember(e => e.Data, opt => opt.ConvertUsing<Base32ToByteStringFormatter, string>());

            cfg.CreateMap<DateTime, Timestamp>().ConvertUsing(s => s.ToTimestamp());
            cfg.CreateMap<Timestamp, DateTime>().ConvertUsing(s => s.ToDateTime());
        }
    }
}
