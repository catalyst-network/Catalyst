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
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Manages the IPFS Configuration.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Configuration values are JSON.  <see href="http://www.newtonsoft.com/json">Json.NET</see>
    ///   is used to represent JSON.
    ///   </para>
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/CONFIG.md">Config API spec</seealso>
    public interface IConfigApi
    {
        /// <summary>
        ///   Gets the entire configuration.
        /// </summary>
        /// <returns>
        ///   A <see cref="JObject"/> containing the configuration.
        /// </returns>
        Task<JObject> GetAsync(CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Gets the value of a configuration key.
        /// </summary>
        /// <param name="key">
        ///   The key name, such as "Addresses.API".
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   The value of the <paramref name="key"/> as <see cref="JToken"/>.
        /// </returns>
        /// <exception cref="Exception">
        ///   When the <paramref name="key"/> does not exist.
        /// </exception>
        /// <remarks>
        ///   Keys are case sensistive.
        /// </remarks>
        Task<JToken> GetAsync(string key, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Adds or replaces a configuration value.
        /// </summary>
        /// <param name="key">
        ///   The key name, such as "Addresses.API".
        /// </param>
        /// <param name="value">
        ///   The new <see cref="string"/> value of the <paramref name="key"/>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        Task SetAsync(string key, string value, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Adds or replaces a configuration value.
        /// </summary>
        /// <param name="key">
        ///   The key name, such as "Addresses.API".
        /// </param>
        /// <param name="value">
        ///   The new <see cref="JToken">JSON</see> value of the <paramref name="key"/>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        Task SetAsync(string key, JToken value, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Replaces the entire configuration.
        /// </summary>
        /// <param name="config"></param>
        Task ReplaceAsync(JObject config);
    }
}
