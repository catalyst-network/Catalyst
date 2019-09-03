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

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Abstractions.Dfs
{
    /// <summary>
    ///   Provides read-write access to a distributed file system.
    /// </summary>
    public interface IDfs
    {
        /// <summary>
        /// Add some text to the distributed file system.
        /// </summary>
        /// <param name="content">The text to add to the DFS.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>The unique ID to the content created on the DFS.</returns>
        Task<string> AddTextAsync(string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the content of an existing file on the DFS as a UTF8 string.
        /// </summary>
        /// <param name="id">The unqiue ID of the content on the DFS.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>The content of the DFS file as a UTF8 encoded string.</returns>
        Task<string> ReadTextAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds content from a stream of data to the DFS.
        /// </summary>
        /// <param name="content">A stream containing the data to be stored on the DFS.</param>
        /// <param name="name">A name for the <paramref name="content"/></param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>The unique ID to the newly added content on the DFS.</returns>
        Task<string> AddAsync(Stream content, string name = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams the content of an existing file on the DFS.
        /// </summary>
        /// <param name="id">The unique ID of the content on the DFS.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>A <see cref="T:System.IO.Stream" /> to the content of the file.</returns>
        /// <remarks>
        ///   The returned <see cref="T:System.IO.Stream" /> must be disposed.
        /// </remarks>
        Task<Stream> ReadAsync(string id, CancellationToken cancellationToken = default);
    }
}
