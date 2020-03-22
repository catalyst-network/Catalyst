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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MultiFormats;

namespace Lib.P2P.Routing
{
    /// <summary>
    ///   Manages a list of content that is provided by multiple peers.
    /// </summary>
    /// <remarks>
    ///   A peer is expected to provide content for at least <see cref="ProviderTtl"/>.
    ///   After this expires the provider is removed from the list.
    /// </remarks>
    public sealed class ContentRouter : IDisposable
    {
        private sealed class ProviderInfo
        {
            /// <summary>
            ///   When the provider entry expires.
            /// </summary>
            public DateTime Expiry { get; set; }

            /// <summary>
            ///   The peer ID of the provider.
            /// </summary>
            public MultiHash PeerId { get; set; }
        }

        private ConcurrentDictionary<string, List<ProviderInfo>> _content =
            new ConcurrentDictionary<string, List<ProviderInfo>>();

        private string Key(Cid cid) { return "/providers/" + cid.Hash.ToBase32(); }

        /// <summary>
        ///   How long a provider is assumed to provide some content.
        /// </summary>
        /// <value>
        ///   Defaults to 24 hours (1 day).
        /// </value>
        public TimeSpan ProviderTtl { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        ///    Adds the <see cref="Cid"/> and <see cref="Peer"/> to the content routing system.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content that the <paramref name="provider"/> contains.
        /// </param>
        /// <param name="provider">
        ///   The peer ID that contains the <paramref name="cid"/>.
        /// </param>
        public void Add(Cid cid, MultiHash provider) { Add(cid, provider, DateTime.Now); }

        /// <summary>
        ///   Adds the <see cref="Cid"/> and <see cref="Peer"/> to the content 
        ///   routing system at the specified <see cref="DateTime"/>.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content that the <paramref name="provider"/> contains.
        /// </param>
        /// <param name="provider">
        ///   The peer ID that contains the <paramref name="cid"/>.
        /// </param>
        /// <param name="now">
        ///   The local time that the <paramref name="provider"/> started to provide
        ///   the <paramref name="cid"/>.
        /// </param>
        public void Add(Cid cid, MultiHash provider, DateTime now)
        {
            var pi = new ProviderInfo
            {
                Expiry = now + ProviderTtl,
                PeerId = provider
            };

            _content.AddOrUpdate(
                Key(cid),
                (key) => new List<ProviderInfo> {pi},
                (key, providers) =>
                {
                    var existing = providers
                       .FirstOrDefault(p => p.PeerId == provider);
                    if (existing != null)
                    {
                        existing.Expiry = pi.Expiry;
                    }
                    else
                    {
                        providers.Add(pi);
                    }
                    
                    return providers;
                });
        }

        /// <summary>
        ///   Gets the providers for the <see cref="Cid"/>.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content.
        /// </param>
        /// <returns>
        ///   A sequence of peer IDs (providers) that contain the <paramref name="cid"/>.
        /// </returns>
        public IEnumerable<MultiHash> Get(Cid cid)
        {
            if (!_content.TryGetValue(Key(cid), out var providers))
            {
                return Enumerable.Empty<MultiHash>();
            }

            return providers
               .Where(p => DateTime.Now < p.Expiry)
               .Select(p => p.PeerId);
        }

        /// <inheritdoc />
        public void Dispose() { }
    }
}
