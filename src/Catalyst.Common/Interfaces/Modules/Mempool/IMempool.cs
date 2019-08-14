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
using Catalyst.Protocol.Transaction;
using MongoDB.Driver.Core.Clusters;

namespace Catalyst.Common.Interfaces.Modules.Mempool
{
    public interface IMempool
    {
        /// <summary>
        ///     Gets a snapshot of the current mempool content.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMempoolDocument> GetMemPoolContent();

        /// <summary>Determines whether the mempool contains document.</summary>
        /// <param name="key"><see cref="TransactionSignature"/> key for the mempool document <seealso cref="IMempoolDocument"/>.</param>
        /// <returns><c>true</c> if the specified key contains document; otherwise, <c>false</c>.</returns>
        bool ContainsDocument(TransactionSignature key);

        /// <summary>
        ///     Gets a snapshot of the current mempool content.
        /// </summary>
        /// <returns></returns>
        List<byte[]> GetMemPoolContentEncoded();

        /// <summary>
        ///     Saves the transaction associated with a given key.
        /// </summary>
        /// <param name="mempoolDocument"></param>
        bool SaveMempoolDocument(IMempoolDocument mempoolDocument);

        /// <summary>
        ///     Retrieves the transaction corresponding the a given key.
        /// </summary>
        /// <param name="key">Key under which the transaction is stored.</param>
        /// <returns>The transaction matching the <see cref="key" /> if any.</returns>
        IMempoolDocument GetMempoolDocument(TransactionSignature key);

        /// <summary>
        /// Clear the mempool content.
        /// </summary>
        void Clear();
    }
}
