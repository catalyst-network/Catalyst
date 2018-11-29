using System.Threading.Tasks;
using System;
using Ipfs.Api;
using System.Threading;
using ADL.Utilities;
using ADL.Node.Interfaces;
using System.Diagnostics;
using ADL.DFS;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ADL.DFS
{
    /// <summary>
    ///   Wrapper for some of the Ipfs methods.
    ///   It will try to connect the client to the IPFS daemon.
    /// </summary>
    public class IpfsConnector : IDFS
    {
        private static IpfsClient _client; 
        private string _defaultApiEndPoint;
        
        private IDfsSettings _settings { get; set; }
        
        /// <summary>
        ///   Check if IPFS client can connect to IPFS daemon
        /// </summary>
        /// <returns>
        ///   Boolean
        /// </returns>
        public bool IsClientConnected()
        {
            if (_client == null)
            {
                // better to throw because there is a problem with creating the
                // instance rather than connecting to the IPFS daemon
                throw new ArgumentNullException(); 
            }

            try
            {
                // Try to get id of this peer. If the daemon is not running than
                // it will throw a socket connection exception.
                var x = _client.DoCommandAsync("id", default(CancellationToken)).Result;
                var j = JObject.Parse(x);
                
                Console.WriteLine("Started IPFS peer ID = " + (j["ID"] != null ? $"{j["ID"]}" : "field not found"));
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void TryToConnectClient()
        {
            var retries = 1;
            
            while (retries <= _settings.ConnectRetries)
            {
                if (!IsClientConnected())
                {
                    Console.WriteLine($"IPFS daemon not running - Trying to connect. Attempt #{retries}");
                    "ipfs daemon".BackgroundCmd(); // invoke as extension method
                }
                else
                {
                    return;
                }

                retries++;
            }
            
           // If it could not connect after a few attempt then throw
           // an invalid operation exception and backup
           throw new InvalidOperationException("Failed to connect with IPFS daemon");
        }

        /// <summary>
        ///   Start IPFS daemon and set client and settings to null
        /// </summary>
        public void Start(IDfsSettings settings)
        {
            if (_client != null)
            {
                TryToConnectClient(); // just to validate that connection with daemon is alive too
                return;
            }

            _client  = new IpfsClient();
            
            _settings = settings;            
            _defaultApiEndPoint = IpfsClient.DefaultApiUri + _settings.IpfsVersionApi;
            
            TryToConnectClient();
        }
        
        /// <summary>
        ///   Stop IPFS daemon and set client and settings to null
        /// </summary>
        public void Stop()
        {
            var localByName = Process.GetProcessesByName("ipfs");
            if (localByName.Length == 1)
            {
                localByName[0].Kill(); // kill daemon process
            }

            if (_client != null)
            {
                if (IsClientConnected()) // if still connected then operation failed
                {
                    throw new InvalidOperationException();
                }
            }

            _client = null;
            _settings = null;
        }
        
        /// <summary>
        ///   Add a file to IPFS.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>
        ///   A string containing the hash of the file just added
        /// </returns>
        /// <remarks>
        ///   It uses curl as a workaround to add a file to IPFS
        /// </remarks>         
        public string AddFile(string filename)
        {
            var cmd = $"curl -F \"file=@{filename}\" {_defaultApiEndPoint}add";
            
            try
            {
                var output = cmd.WaitForCmd();
                dynamic json = JsonConvert.DeserializeObject(output);

                return json.Hash;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///   Read a file from IPFS
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>
        ///   A Task object containing the text of the file just read.
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
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
