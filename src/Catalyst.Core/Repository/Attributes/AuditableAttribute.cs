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
using Catalyst.Abstractions.Attributes;
using SharpRepository.Repository.Aspects;

namespace Catalyst.Core.Repository.Attributes
{
    public sealed class AuditAttribute : RepositoryActionBaseAttribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="context"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        public override bool OnAddExecuting<T, TKey>(T entity, RepositoryActionContext<T, TKey> context)
        {
            if (entity is IAuditable tmp)
            {
                tmp.Created = DateTime.UtcNow;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="context"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <returns></returns>
        public override bool OnUpdateExecuting<T, TKey>(T entity, RepositoryActionContext<T, TKey> context)
        {
            if (entity is IAuditable tmp)
            {
                tmp.Modified = DateTime.UtcNow;
            }

            return true;
        }
    }
}
