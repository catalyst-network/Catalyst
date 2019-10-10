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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Wire;
using Google.Protobuf.WellKnownTypes;
using MongoDB.Bson.Serialization.Attributes;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Lib.DAO
{
    [BsonIgnoreExtraElements]
    public class TransactionBroadcastDao : DaoBase<TransactionBroadcast, TransactionBroadcastDao>
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
        public IEnumerable<ContractEntryDao> ContractEntries { get; set; }

        public bool IsContractDeployment { get; set; }
        public bool IsContractCall { get; set; }
        public bool IsPublicTransaction { get; set; }
        public bool IsConfidentialTransaction { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<TransactionBroadcast, TransactionBroadcastDao>();
            cfg.AllowNullDestinationValues = true;

            cfg.CreateMap<TransactionBroadcastDao, TransactionBroadcast>()
               .ForMember(e => e.PublicEntries, opt => opt.UseDestinationValue())
               .ForMember(e => e.ContractEntries, opt => opt.UseDestinationValue())
               .ForMember(e => e.ConfidentialEntries, opt => opt.UseDestinationValue());

            cfg.CreateMap<DateTime, Timestamp>().ConvertUsing(s => s.ToTimestamp());
            cfg.CreateMap<Timestamp, DateTime>().ConvertUsing(s => s.ToDateTime());
        }

        public UInt256 SummedEntryFees()
        {
            var sum = ContractEntries.Sum(e => UInt256.Parse(e.Base.TransactionFees))
              + PublicEntries.Sum(e => UInt256.Parse(e.Base.TransactionFees))
              + ConfidentialEntries.Sum(e => UInt256.Parse(e.Base.TransactionFees));
            return sum;
        }

        public bool HasValidEntries()
        {
            return IsContractDeployment ^ IsContractCall ^ IsPublicTransaction ^ IsConfidentialTransaction;
        }
    }
}
