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

using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   A basic Put/Get interface.
    /// </summary>
    public interface IValueStore
    {
        /// <summary>
        ///   Gets th value of a key.
        /// </summary>
        /// <param name="key">
        ///   A byte array representing the name of a key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation that returns
        ///   the value of the key as a byte array.
        /// </returns>
        Task<byte[]> GetAsync(byte[] key, CancellationToken cancel = default);

        /// <summary>
        ///   Tries to get the value of a key.
        /// </summary>
        /// <param name="key">
        ///   A byte array representing the name of a key.
        /// </param>
        /// <param name="value">
        ///   A byte array containing the value of the <paramref name="key"/>
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation that returns
        ///   <b>true</b> if the key exists; otherwise, <b>false</b>.
        /// </returns>
        Task<bool> TryGetAsync(byte[] key, out byte[] value, CancellationToken cancel = default);

        /// <summary>
        ///   Put the value of a key.
        /// </summary>
        /// <param name="key">
        ///   A byte array representing the name of a key.
        /// </param>
        /// <param name="value">
        ///   A byte array containing the value of the <paramref name="key"/>
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PutAsync(byte[] key, out byte[] value, CancellationToken cancel = default);
    }
}
