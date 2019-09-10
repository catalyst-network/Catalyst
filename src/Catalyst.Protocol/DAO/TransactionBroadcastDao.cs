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
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Catalyst.Common;
using Catalyst.Common.Util;
using Catalyst.Protocol.Converters;
using Catalyst.Protocol.Extensions;

namespace Catalyst.Protocol.DAO
{
    public class TransactionBroadcastDao : DaoBase
    {
        public UInt32 Version { get; set; }
        public UInt64 TransactionFees { get; set; }
        public UInt64 LockTime { get; set; }
        public IEnumerable<STTransactionEntryDao> STEntries { get; set; }
        public IEnumerable<CFTransactionEntryDao> CFEntries { get; set; }
        public string Signature { get; set; }
        public IEnumerable<EntryRangeProofDao> EntryRangeProofs { get; set; }
        public TransactionType TransactionType { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Data { get; set; }
        public string From { get; set; }
        public string Init { get; set; }

        public TransactionBroadcastDao()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TransactionBroadcast, TransactionBroadcastDao>().ReverseMap();
                cfg.CreateMap<STTransactionEntry, STTransactionEntryDao>().ReverseMap();
                cfg.CreateMap<CFTransactionEntry, CFTransactionEntryDao>().ReverseMap();

                cfg.CreateMap<EntryRangeProof, EntryRangeProofDao>().ReverseMap();

                cfg.CreateMap<TransactionBroadcast, TransactionBroadcastDao>()
                   .ForMember(d => d.From, opt => opt.ConvertUsing(new KeyUtilsFormatter(), s => s.From.ToByteArray())).ReverseMap();
                
                cfg.CreateMap<DateTime, Timestamp>().ConvertUsing(s => s.ToTimestamp());

                cfg.CreateMap<Timestamp, DateTime>().ConvertUsing(s => s.ToDateTime());

                cfg.CreateMap<ByteString, string>().ConvertUsing(s => s.ToBase64());
                cfg.CreateMap<string, ByteString>().ConvertUsing(s => ByteString.FromBase64(s));

                bool IsToRepeatedField(PropertyMap pm)
                {
                    if (pm.DestinationType.IsConstructedGenericType)
                    {
                        var destGenericBase = pm.DestinationType.GetGenericTypeDefinition();
                        return destGenericBase == typeof(RepeatedField<>);
                    }

                    return false;
                }

                cfg.ForAllPropertyMaps(IsToRepeatedField, (propertyMap, opts) => opts.UseDestinationValue());
            });

            Mapper = config.CreateMapper();
        }

        public override IMessage ToProtoBuff()
        {
            return (IMessage) Mapper.Map<TransactionBroadcast>(this);
        }

        public override DaoBase ToDao(IMessage protoBuff)
        {
            return Mapper.Map<TransactionBroadcastDao>((TransactionBroadcast) protoBuff);
        }
    }
}
