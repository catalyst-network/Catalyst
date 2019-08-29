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
using System.Linq;
using Catalyst.Core.Util;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using SharpRepository.AzureDocumentDb;
using SharpRepository.Repository.Caching;

namespace Catalyst.Core.Repository
{
    public class CosmosDbRepository<T> : DocumentDbRepository<T, string>
        where T : class, new()
    {
        public new DocumentClient Client => base.Client;

        public new Database Database => base.Database;

        public CosmosDbRepository(string endpointUrl,
            string authorizationKey,
            string databaseId,
            bool createIfNotExists,
            string collectionId = null,
            ICachingStrategy<T, string> cachingStrategy = null) :
            base(endpointUrl, authorizationKey, databaseId, createIfNotExists, collectionId, cachingStrategy)
        {
            base.Client.Dispose();

            var client = new DocumentClient(new Uri(endpointUrl), authorizationKey,
                new JsonSerializerSettings
                {
                    Converters = JsonConverterProviders.Converters.ToList(),
                    NullValueHandling = NullValueHandling.Ignore
                });
            base.Client = client;
        }
    }
}
