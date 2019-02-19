using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Catalyst.Node.Common;
using Catalyst.Node.Core.Helpers;
using Ipfs.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Catalyst.Node.Core.Components.Ipfs
{
    /// <summary>
    ///     Wrapper for some of the Catalyst.Components.Ipfs methods.
    ///     It will try to connect the client to the IPFS daemon.
    /// </summary>
    public class IpfsConnector : IDisposable, IIpfs
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private IpfsClient _client;
        private const string DefaultApiEndPoint = "127.0.0.1";

        // <summary>
        //   Start IPFS daemon and set client and settings to null
        // </summary>
        public void CreateIpfsClient(string ipfsVersionApi, int connectRetries)
        {
            if (_client != null)
            {
                TryToConnectClient(connectRetries); // just to validate that connection with daemon is alive too
                return;
            }

            _client = new IpfsClient();

            //_defaultApiEndPoint = IpfsClient.DefaultApiUri + ipfsVersionApi;

            TryToConnectClient(connectRetries);
        }

        /// <summary>
        ///     Stop IPFS daemon and set client and settings to null
        /// </summary>
        public void DestroyIpfsClient()
        {
            var localByName = Process.GetProcessesByName("ipfs");
            if (localByName.Length == 1) {
                localByName[0].Kill(); // kill daemon process
            }

            if (_client != null)
            {
                if (IsClientConnected())
                {
                    throw new InvalidOperationException();
                }                
            }

            _client = null;
        }

        /// <summary>
        ///     Add a file to IPFS.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>
        ///     A string containing the hash of the file just added
        /// </returns>
        /// <remarks>
        ///     It uses curl as a workaround to add a file to IPFS
        /// </remarks>
        public string AddFile(string filename)
        {
            var cmd = $"curl -F \"file=@{filename}\" {DefaultApiEndPoint}add";

            try
            {
                var output = cmd.WaitForCmd();
                dynamic json = JsonConvert.DeserializeObject(output);

                return json.Hash;
            }
            catch (Exception e)
            {
                Logger.Error(e, "AddFile");
                throw;
            }
        }

        /// <summary>
        ///     Read a file from IPFS
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>
        ///     A Task object containing the text of the file just read.
        /// </returns>
        public async Task<string> ReadAllTextAsync(string hash)
        {
            try
            {
                var text = await _client.FileSystem.ReadAllTextAsync(hash);
                return text;
            }
            catch (Exception e)
            {
                Logger.Error(e, "ReadAllTextAsync");
                throw;
            }
        }

        /// <summary>
        ///     Check if IPFS client can connect to IPFS daemon
        /// </summary>
        /// <returns>
        ///     Boolean
        /// </returns>
        public bool IsClientConnected()
        {
            if (_client == null)
            {
                throw new ArgumentNullException();
            }

            try
            {
                // Try to get id of this peer. If the daemon is not running than
                // it will throw a socket connection exception.
                var x = _client.DoCommandAsync("id", default).Result;
                var j = JObject.Parse(x);

                Logger.Information("Started IPFS peer ID = " + (j["ID"] != null ? $"{j["ID"]}" : "field not found"));
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void TryToConnectClient(int connectRetries)
        {
            var retries = 1;
            Logger.Debug($"Connect retries {connectRetries}");
            while (retries <= connectRetries)
            {
                if (!IsClientConnected())
                {
                    Logger.Warning("IPFS daemon not running - Trying to connect. Attempt #{0}", retries);
                    "ipfs daemon".BackgroundCmd(); // invoke as extension method
                }
                else
                {
                    Logger.Information("IPFS daemon connected");
                    return;
                }

                retries++;
            }

            // If it could not connect after a few attempt then throw
            // an invalid operation exception and backup
            throw new InvalidOperationException("Failed to connect with IPFS daemon");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) { DestroyIpfsClient(); }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}