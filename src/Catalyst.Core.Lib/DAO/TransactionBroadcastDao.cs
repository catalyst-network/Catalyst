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
using System.Linq;
using AutoMapper;
using Catalyst.Abstractions.DAO;
using Catalyst.Abstractions.Mempool.Models;
using Catalyst.Core.Lib.DAO.Cryptography;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MongoDB.Bson.Serialization.Attributes;
using TheDotNetLeague.MultiFormats.MultiBase;

namespace Catalyst.Core.Lib.DAO
{
    /// <summary>
    /// @TODO we shouldnt be saving TransactionBroadcast, this is a wire only object,
    ///     this should be mapped to a mempool object
    /// </summary>
    [BsonIgnoreExtraElements]
    public class TransactionBroadcastDao : DaoBase
    {
        private SignatureDao _signature;

        public SignatureDao Signature
        {
            get => _signature;
            set
            {
                _signature = value;
                Id = value.RawBytes;
            }
        }

        public DateTime TimeStamp { get; set; }
        public IEnumerable<PublicEntryDao> PublicEntries { get; set; }
        public IEnumerable<ConfidentialEntryDao> ConfidentialEntries { get; set; }

        public IEnumerable<MempoolItem> ToMempoolItems(IMapperProvider mapperProvider)
        {
            var mempoolItems = this.PublicEntries.Select(x =>
            {
                var mempoolItem = x.ToMempoolItem(mapperProvider);
                mempoolItem.Signature = Signature.ToProtoBuff<SignatureDao, Signature>(mapperProvider).ToByteArray().ToBase32();
                return mempoolItem;
            }).ToList();

            return mempoolItems;
        }
    }

    public class TransactionBroadcastMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<TransactionBroadcast, TransactionBroadcastDao>();
            cfg.AllowNullDestinationValues = true;

            cfg.CreateMap<TransactionBroadcastDao, TransactionBroadcast>()
               .ForMember(e => e.PublicEntries, opt => opt.UseDestinationValue())
               .ForMember(e => e.ConfidentialEntries, opt => opt.UseDestinationValue());

            cfg.CreateMap<DateTime, Timestamp>().ConvertUsing(s => s.ToTimestamp());
            cfg.CreateMap<Timestamp, DateTime>().ConvertUsing(s => s.ToDateTime());

            cfg.CreateMap<PublicEntryDao, MempoolItem>()
            .ForMember(d => d.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(d => d.Data, opt => opt.MapFrom(src => src.Amount))
            .ForMember(d => d.Fee, opt => opt.MapFrom(src => src.Base.TransactionFees))
            .ForMember(d => d.Nonce, opt => opt.MapFrom(s => s.Base.Nonce))
            .ForMember(d => d.SenderAddress, opt => opt.MapFrom(src => src.Base.SenderPublicKey))
            .ForMember(d => d.ReceiverAddress, opt => opt.MapFrom(src => src.Base.ReceiverPublicKey));

            cfg.CreateMap<MempoolItem, PublicEntry>()
            .ForMember(d => d.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(d => d.Data, opt => opt.MapFrom(src => src.Amount))
            .ForMember(d => d.Base.TransactionFees, opt => opt.MapFrom(src => src.Fee))
            .ForMember(d => d.Base.Nonce, opt => opt.MapFrom(s => s.Nonce))
            .ForMember(d => d.Base.SenderPublicKey, opt => opt.MapFrom(src => src.SenderAddress))
            .ForMember(d => d.Base.ReceiverPublicKey, opt => opt.MapFrom(src => src.ReceiverAddress));
        }
    }
}
