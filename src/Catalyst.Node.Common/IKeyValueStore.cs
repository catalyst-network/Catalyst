using System;
using System.Collections.Generic;

namespace Catalyst.Node.Common
{
    public interface IKeyValueStore
    {
        /// <summary>
        ///     Sets the <see cref="value" /> for the given <see cref="key" /> if it doesn't exist yet in the store.
        /// </summary>
        /// <param name="key">The key under which the value needs to be stored.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="expiry">The time for which the record should be held in store.</param>
        /// <returns>True if the value has been stored. False otherwise.</returns>
        bool Set(byte[] key, byte[] value, TimeSpan? expiry);

        /// <summary>
        ///     Returns the value stored at the given <see cref="key" /> if it is found in the store.
        /// </summary>
        byte[] Get(byte[] key);

        /// <summary>
        ///     Get a snapshot of all the values currently in store.
        /// </summary>
        IDictionary<byte[], byte[]> GetSnapshot();
    }
}