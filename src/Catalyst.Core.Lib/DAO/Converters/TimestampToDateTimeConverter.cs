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
using AutoMapper;
using Catalyst.Abstractions.DAO;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Core.Lib.DAO.Converters 
{
    public class TimestampToDateTimeInitializer : IMapperInitializer
    {
        public void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Timestamp, DateTime>().ConvertUsing<TimestampToDateTimeConverter>();
            cfg.CreateMap<DateTime, Timestamp>().ConvertUsing<TimestampToDateTimeConverter>();
        }

        public class TimestampToDateTimeConverter : ITypeConverter<Timestamp, DateTime>, ITypeConverter<DateTime, Timestamp>
        {
            public DateTime Convert(Timestamp source, DateTime destination, ResolutionContext context) => source.ToDateTime();

            public Timestamp Convert(DateTime source, Timestamp destination, ResolutionContext context) => Timestamp.FromDateTime(source);
        }
    }
}
