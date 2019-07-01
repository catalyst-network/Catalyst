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

using System.Collections.Generic;
using Dawn;

namespace Catalyst.Common.Interfaces.Registry
{
    public class RegistryBase<T, K> : IRegistryBase<T, K>
        where K : class
    {
        public IDictionary<T, K> Registry { get; protected set; }

        public bool AddItemToRegistry(T identifier, K item)
        {
            Guard.Argument(item, nameof(item)).NotNull();
            return Registry.TryAdd(identifier, item);
        }

        public K GetItemFromRegistry(T identifier)
        {
            var retItem = Registry.TryGetValue(identifier, out var item)
                ? item
                : null;
            return retItem;
        }

        /// <inheritdoc />
        public bool RemoveItemFromRegistry(T identifier)
        {
            //Guard.Argument(socketHashCode, nameof(socketHashCode)).NotZero();
            return Registry.Remove(identifier);
        }

    }


}
