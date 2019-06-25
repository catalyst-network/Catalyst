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
