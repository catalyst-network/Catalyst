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

using Google.Protobuf;
using SharpRepository.Repository;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Catalyst.Abstractions.DAO;

namespace Catalyst.Core.Lib.DAO
{
    public abstract class DaoBase<TOriginal, TDao> : IMapperInitializer
    {
        [RepositoryPrimaryKey(Order = 1)]
        [Key]
        public string Id { get; set; }

        public TOriginal ToProtoBuff()
        {
            return MapperProvider.MasterMapper.Map<TOriginal>(this);
        }

        public TDao ToDao(TOriginal protoBuff)
        {
            return MapperProvider.MasterMapper.Map<TDao>(protoBuff);
        }

        public abstract void InitMappers(IMapperConfigurationExpression cfg);
    }
}
