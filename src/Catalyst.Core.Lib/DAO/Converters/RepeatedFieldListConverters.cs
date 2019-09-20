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
using Google.Protobuf.Collections;

namespace Catalyst.Core.Lib.DAO.Converters
{
    public class RepeatedFieldExtendedToListConverter<TIn, TOut, TInnerConverter>
        : IValueConverter<RepeatedField<TIn>, List<TOut>>
        where TInnerConverter : IValueConverter<TIn, TOut>
    {
        private readonly TInnerConverter _innerConverter = Activator.CreateInstance<TInnerConverter>();

        public List<TOut> Convert(RepeatedField<TIn> sourceMember, ResolutionContext context)
        {
            return sourceMember.Select(i => _innerConverter.Convert(i, context)).ToList();
        }
    }

    public class ListToRepeatedFieldExtendedConverter<TIn, TOut, TInnerConverter>
        : IValueConverter<List<TIn>, RepeatedField<TOut>>
        where TInnerConverter : IValueConverter<TIn, TOut>
    {
        private readonly TInnerConverter _innerConverter = Activator.CreateInstance<TInnerConverter>();

        public RepeatedField<TOut> Convert(List<TIn> sourceMember, ResolutionContext context)
        {
            var result = new RepeatedField<TOut>();
            sourceMember.ForEach(i => result.Add(_innerConverter.Convert(i, context)));
            return result;
        }
    }
    public class RepeatedFieldToListConverter<TIn, TOut, TInnerConverter>
        : IValueConverter<RepeatedField<TIn>, List<TOut>>
        where TInnerConverter : IValueConverter<TIn, TOut>
    {
        private readonly TInnerConverter _innerConverter = Activator.CreateInstance<TInnerConverter>();

        public List<TOut> Convert(RepeatedField<TIn> sourceMember, ResolutionContext context)
        {
            return sourceMember.Select(i => _innerConverter.Convert(i, context)).ToList();
        }
    }

    public class ListToRepeatedFieldConverter<TIn, TOut, TInnerConverter>
        : IValueConverter<List<TIn>, RepeatedField<TOut>>
        where TInnerConverter : IValueConverter<TIn, TOut>
    {
        private readonly TInnerConverter _innerConverter = Activator.CreateInstance<TInnerConverter>();

        public RepeatedField<TOut> Convert(List<TIn> sourceMember, ResolutionContext context)
        {
            var result = new RepeatedField<TOut>();
            sourceMember.ForEach(i => result.Add(_innerConverter.Convert(i, context)));
            return result;
        }
    }
}
