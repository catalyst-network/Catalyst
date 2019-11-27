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
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Protocol.Wire;
using MongoDB.Bson.Serialization.Attributes;

namespace Catalyst.Core.Lib.DAO
{
    /// <summary>
    /// @TODO we shouldnt be saving TransactionBroadcast, this is a wire only object,
    ///     this should be mapped to a mempool object
    /// </summary>
    [BsonIgnoreExtraElements]
    public class TransactionBroadcastDao : DaoBase
    {
        public PublicEntryDao PublicEntry { get; set; }
    }

    public class TransactionBroadcastMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<TransactionBroadcast, TransactionBroadcastDao>();
            cfg.AllowNullDestinationValues = true;

            cfg.CreateMap<TransactionBroadcastDao, TransactionBroadcast>()
               .ForMember(e => e.PublicEntry, opt => opt.UseDestinationValue());
        }
    }
}
