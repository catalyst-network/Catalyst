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
using AutoMapper;
using Catalyst.Abstractions.DAO;
using Catalyst.Abstractions.Mempool.Models;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Protocol.Transaction;

namespace Catalyst.Core.Lib.Mempool.Models
{
    public class MempoolItem : IMempoolItem
    {
        public byte[] Signature { set; get; }
        public DateTime Timestamp { set; get; }
        public byte[] Amount { set; get; }
        public long Nonce { set; get; }
        public byte[] ReceiverAddress { set; get; }
        public byte[] SenderAddress { set; get; }
        public byte[] Fee { set; get; }
        public byte[] Data { set; get; }
    }

    public sealed class MempoolItemMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            {
                cfg.CreateMap<TransactionBroadcastDao, MempoolItem[]>().ForMember(x=>x.)

                cfg.CreateMap<MempoolItem, PublicEntry>()
                   .ForMember(d => d.Amount,
                        opt => opt.ConvertUsing(new UInt256StringToByteStringConverter(), s => s.Amount))
                   .ForMember(e => e.Data, opt => opt.ConvertUsing<StringBase64ToByteStringConverter, string>());
            }
        }
    }
}
