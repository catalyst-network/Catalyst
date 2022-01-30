#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace Catalyst.Core.Lib.FileSystem
{
    /// <summary>
    ///     A file based repository for name value pairs.
    /// </summary>
    /// <typeparam name="TName">
    ///     The type used for a unique name.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///     The type used for the value.
    /// </typeparam>
    /// <remarks>
    ///     All operations are atomic, a reader/writer lock is used.
    /// </remarks>
    public class InMemoryStore<TName, TValue> : IStore<TName, TValue>
        where TValue : class
    {
        private IDictionary<TName, TValue> _memoryStore = new Dictionary<TName, TValue>();

        /// <summary>
        ///     The fully qualififed path to a directory
        ///     that stores the name value pairs.
        /// </summary>
        /// <value>
        ///     A fully qualified path.
        /// </value>
        /// <remarks>
        ///     The directory must already exist.
        /// </remarks>
        public string Folder { get; set; }

        /// <summary>
        ///     A function that converts the name to a case insensitive key name.
        /// </summary>
        public Func<TName, string> NameToKey { get; set; }

        /// <summary>
        ///     A function that converts the case insensitive key to a name.
        /// </summary>
        public Func<string, TName> KeyToName { get; set; }

        public IEnumerable<TValue> Values => _memoryStore.Values;

        /// <summary>
        ///     Try to get the value with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The unique name of the entity.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     a <typeparamref name="TValue" /> or <b>null</b> if the <paramref name="name" />
        ///     does not exist.
        /// </returns>
        public async Task<TValue> TryGetAsync(TName name, CancellationToken cancel = default)
        {
            if (_memoryStore.ContainsKey(name))
            {
                return _memoryStore[name];
            }

            return null;
        }

        /// <summary>
        ///     Get the value with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The unique name of the entity.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     a <typeparamref name="TValue" />
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     When the <paramref name="name" /> does not exist.
        /// </exception>
        public async Task<TValue> GetAsync(TName name, CancellationToken cancel = default)
        {
            var value = await TryGetAsync(name, cancel).ConfigureAwait(false);
            if (value == null)
            {
                throw new KeyNotFoundException($"Missing '{name}'.");
            }

            return value;
        }

        /// <summary>
        ///     Put the value with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The unique name of the entity.
        /// </param>
        /// <param name="value">
        ///     The entity.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///     If <paramref name="name" /> already exists, it's value is overwriten.
        ///     <para>
        ///         The file is deleted if an exception is encountered.
        ///     </para>
        /// </remarks>
        public async Task PutAsync(TName name, TValue value, CancellationToken cancel = default)
        {
            _memoryStore.Add(name, value);
        }

        /// <summary>
        ///     Remove the value with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The unique name of the entity.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///     A non-existent <paramref name="name" /> does nothing.
        /// </remarks>
        public async Task RemoveAsync(TName name, CancellationToken cancel = default)
        {
            _memoryStore.Remove(name);
        }
    }
}
