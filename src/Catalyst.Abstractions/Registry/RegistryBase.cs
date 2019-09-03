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
using Catalyst.Abstractions.Enumerator;
using Dawn;

namespace Catalyst.Abstractions.Registry
{
    public abstract class RegistryBase<TKey, TValue> : IRegistryBase<TKey, TValue>
        where TKey : Enumeration
        where TValue : class
    {
        protected IDictionary<TKey, TValue> Registry { get; set; }

        public bool AddItemToRegistry(TKey identifier, TValue item)
        {
            Guard.Argument(item, nameof(item)).NotNull();
            return Registry.TryAdd(identifier, item);
        }

        public TValue GetItemFromRegistry(TKey identifier)
        {
            Registry.TryGetValue(identifier, out var item);
            return item;
        }

        public bool RegistryContainsKey(TKey identifier) { return Registry.ContainsKey(identifier); }

        public bool RemoveItemFromRegistry(TKey identifier)
        {
            return Registry.Remove(identifier);
        }

        void IDisposable.Dispose()
        {
            foreach (var registryValue in Registry.Values)
            {
                if (registryValue is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
