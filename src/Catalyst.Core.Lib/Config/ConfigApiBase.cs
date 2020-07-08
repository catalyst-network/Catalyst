using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Lib.Config
{
    public abstract class ConfigApiBase : IConfigApi
    {
        private JObject _configuration;
        private readonly string _filePath;

        protected ConfigApiBase(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<JObject> GetAsync(CancellationToken cancel = default)
        {
            // If first time, load the configuration into memory.
            if (_configuration != null)
            {
                return _configuration;
            }

            if (File.Exists(_filePath))
            {
                using var reader = File.OpenText(_filePath);
                using var jtr = new JsonTextReader(reader);
                _configuration = await JObject.LoadAsync(jtr, cancel).ConfigureAwait(false);
            }
            else
            {
                await OnFileNotExisting().ConfigureAwait(false);
            }

            return _configuration;
        }

        protected abstract Task OnFileNotExisting();

        public async Task<JToken> GetAsync(string key, CancellationToken cancel = default)
        {
            JToken config = await GetAsync(cancel).ConfigureAwait(false);
            var keys = key.Split('.');
            foreach (var name in keys)
            {
                config = config[name];
                if (config == null)
                {
                    throw new KeyNotFoundException($"Configuration setting '{key}' does not exist.");
                }
            }

            return config;
        }

        public Task ReplaceAsync(JObject config)
        {
            _configuration = config ?? throw new ArgumentNullException(nameof(config));
            return SaveAsync();
        }

        public Task SetAsync(string key, string value, CancellationToken cancel = default)
        {
            return SetAsync(key, JToken.FromObject(value), cancel);
        }

        public async Task SetAsync(string key, JToken value, CancellationToken cancel = default)
        {
            var config = await GetAsync(cancel).ConfigureAwait(false);

            // If needed, create the setting owner keys.
            var keys = key.Split('.');
            foreach (var name in keys.Take(keys.Length - 1))
            {
                if (!(config[name] is JObject token))
                {
                    token = new JObject();
                    config[name] = token;
                }

                config = token;
            }

            config[keys.Last()] = value;
            await SaveAsync().ConfigureAwait(false);
        }

        private async Task SaveAsync()
        {
            await using var fs = File.OpenWrite(_filePath);
            await using var writer = new StreamWriter(fs);
            using var jtw = new JsonTextWriter(writer)
            {
                Formatting = Formatting.Indented
            };
            await _configuration.WriteToAsync(jtw).ConfigureAwait(false);
        }
    }
}
