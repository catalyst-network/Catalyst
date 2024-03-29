#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Protocol.Deltas;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Core.Lib.DAO.Deltas
{
    public class DeltaDao : DaoBase
    {
        public UInt32 Version { get; set; }
        public string PreviousDeltaDfsHash { get; set; }
        public string MerkleRoot { get; set; }
        public string MerklePoda { get; set; }
        public DateTime TimeStamp { get; set; }
        public List<PublicEntryDao> StEntries { get; set; }
        public List<ConfidentialEntryDao> CfEntries { get; set; }
        public List<CoinbaseEntryDao> CbEntries { get; set; }
    }

    public class DeltaMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Delta, DeltaDao>().ReverseMap();
            cfg.CreateMap<DateTime, Timestamp>().ConvertUsing(s => s.ToTimestamp());
            cfg.CreateMap<Timestamp, DateTime>().ConvertUsing(s => s.ToDateTime());

            cfg.CreateMap<Delta, DeltaDao>()
               .ForMember(e => e.PreviousDeltaDfsHash,
                    opt => opt.ConvertUsing<ByteStringToDfsHashConverter, ByteString>())
               .ForMember(d => d.MerkleRoot,
                    opt => opt.ConvertUsing<ByteStringToDfsHashConverter, ByteString>())
               .ForMember(d => d.MerklePoda,
                    opt => opt.ConvertUsing<ByteStringToDfsHashConverter, ByteString>());

            cfg.CreateMap<DeltaDao, Delta>()
               .ForMember(e => e.PreviousDeltaDfsHash,
                    opt => opt.ConvertUsing<DfsHashToByteStringConverter, string>())
               .ForMember(d => d.MerkleRoot,
                    opt => opt.ConvertUsing<DfsHashToByteStringConverter, string>())
               .ForMember(d => d.MerklePoda,
                    opt => opt.ConvertUsing<DfsHashToByteStringConverter, string>());
        }
    }
}
