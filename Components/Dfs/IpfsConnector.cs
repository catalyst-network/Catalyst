using System.Threading.Tasks;
using System;
using System.Net.Sockets;
using Ipfs.Api;
using System.Threading;
using ADL.DFS.Helpers;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ADL.DFS
{
    /// <summary>
    ///   Wrapper for some of the Ipfs methods.
    ///   It will try to connect the client to the IPFS daemon.
    /// </summary>
    public class IpfsConnector :IDFS
    {
        private readonly IpfsClient _client; 
        private readonly string _defaultApiEndPoint;
        
        private bool ClientConnected()
        {
            if (_client == null)
            {
                // better to throw as there is a problem with creating the
                // instance rather than connecting to the IPFS daemon
                throw new ArgumentNullException(); 
            }

            try
            {
                // Try to get id of this peer. If the daemon is not running than
                // it will throw a socket connection exception. This is just to make
                // sure that the environment is set-up correctly
                var x = _client.DoCommandAsync("id", default(CancellationToken)).Result;

                // It will throw if the json object is not well formed.
                var j = JObject.Parse(x);

                // Just to give an hint that the peer is up and running and has an ID
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
            
            while (retries <= 10)
            {
                if (!ClientConnected())
                {
                    Console.WriteLine($"IPFS daemon not running - Trying to connect. Attempt #{retries}");
                    "ipfs daemon".BackgroundCmd(); // invoke as extension method
                }
                else
                {
                    return;
                }

                retries--;
            }
            
           // If it could not connect after a few attempt then throw
           // a socket exception and backup
           throw new SocketException();
        }
        
        /// <summary>
        ///   Default constructor for IpfsConnector. It will attempt to to connect to the daemon
        ///   It will throw a SocketException if it cannot connect.
        /// </summary>
        public IpfsConnector()
        {
            _client  = new IpfsClient();
            _defaultApiEndPoint = IpfsClient.DefaultApiUri + "api/v0/";

            TryToConnectClient();
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
        /// <remarks>
        ///   It is an async method and uses IPFS.Api
        /// </remarks>         
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
