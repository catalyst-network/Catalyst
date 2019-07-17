using Catalyst.Common.Util;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using SharpRepository.AzureDocumentDb;
using SharpRepository.Repository.Caching;
using System;
using System.Collections.Generic;

namespace Catalyst.Common.Repository
{
    public class CosmosDbRepository<T, TKey> : DocumentDbRepository<T, TKey>
        where T : class, new()
    {
        private DocumentClient _client;

        public new DocumentClient Client => base.Client;

        public new Database Database => base.Database;

        public CosmosDbRepository(string endpointUrl,
            string authorizationKey,
            string databaseId,
            bool createIfNotExists,
            string collectionId = null,
            ICachingStrategy<T, TKey> cachingStrategy = null) :
            base(endpointUrl, authorizationKey, databaseId, createIfNotExists, collectionId, cachingStrategy)
        {
            base.Client.Dispose();
            var converters = new List<JsonConverter>
            {
                new IpEndPointConverter(), new IpAddressConverter()
            };
            _client = new DocumentClient(new Uri(endpointUrl), authorizationKey,
                new JsonSerializerSettings()
                {
                    Converters = converters,
                    NullValueHandling = NullValueHandling.Ignore
                });
            base.Client = _client;
        }
    }
}
