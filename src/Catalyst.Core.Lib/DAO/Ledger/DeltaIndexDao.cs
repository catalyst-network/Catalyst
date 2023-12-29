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

using AutoMapper;
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Protocol.Deltas;
using Google.Protobuf;
using Newtonsoft.Json;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.DAO.Ledger
{
    public class DeltaIndexDao
    {
        [RepositoryPrimaryKey(Order = 1)]
        [JsonProperty("id")]
        public string Id => BuildDocumentId(Height);
        public ulong Height { set; get; }
        public string Cid { set; get; }

        public static string BuildDocumentId(ulong number) => number.ToString("D");

        public static TProto ToProtoBuff<TProto>(DeltaIndexDao dao, IMapperProvider mapperProvider)
            where TProto : IMessage
        {
            return mapperProvider.Mapper.Map<TProto>(dao);
        }

        public static DeltaIndexDao ToDao<TProto>(DeltaIndex protoBuff, IMapperProvider mapperProvider)
            where TProto : IMessage
        {
            return mapperProvider.Mapper.Map<DeltaIndexDao>(protoBuff);
        }
    }

    public class DeltaIndexMapperInitialiser : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<DeltaIndex, DeltaIndexDao>()
               .ForMember(a => a.Height, opt => opt.UseDestinationValue())
               .ForMember(a => a.Cid,
                    opt => opt.ConvertUsing<ByteStringToDfsHashConverter, ByteString>());

            cfg.CreateMap<DeltaIndexDao, DeltaIndex>()
               .ForMember(a => a.Height, opt => opt.UseDestinationValue())
               .ForMember(a => a.Cid,
                    opt => opt.ConvertUsing<DfsHashToByteStringConverter, string>());
        }
    }
}
