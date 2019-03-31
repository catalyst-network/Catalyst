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

namespace Catalyst.Node.Common.Interfaces
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