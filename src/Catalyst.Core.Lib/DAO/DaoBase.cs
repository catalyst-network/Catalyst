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

using SharpRepository.Repository;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Catalyst.Abstractions.DAO;
using Catalyst.Abstractions.Repository;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Catalyst.Core.Lib.DAO
{
    public abstract class DaoBase<TProto, TDao> : IMapperInitializer, 
        IValueConverter<TProto, TDao>, IDocument
    {
        [Key]
        [RepositoryPrimaryKey(Order = 1)]
        [JsonProperty("_id")]
        public string DocumentId { get; set; }

        public TProto ToProtoBuff()
        {
            return MapperProvider.MasterMapper.Map<TProto>(this);
        }

        public TDao ToDao(TProto protoBuff)
        {
            return MapperProvider.MasterMapper.Map<TDao>(protoBuff);
        }

        public abstract void InitMappers(IMapperConfigurationExpression cfg);

        public TDao Convert(TProto sourceMember, ResolutionContext context)
        {
            return ToDao(sourceMember);
        }

        public TProto Convert(TDao sourceMember, ResolutionContext context)
        {
            return MapperProvider.MasterMapper.Map<TProto>(sourceMember);
        }
    }
}
