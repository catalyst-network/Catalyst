using System.Threading.Tasks;
using System;
using System.Net.Sockets;
using Ipfs.Api;
using System.Threading;
using ADL.DFS.Helpers;
using Newtonsoft.Json.Linq;

namespace ADL.DFS
{
    /// <summary>
    ///   Wrapper for some of the Ipfs methods.
    ///   It will try to connect the client to the IPFS daemon.
    /// </summary>
    public class IpfsConnector :IDFS
    {
        private static readonly IpfsClient _client = new IpfsClient();
        
        private bool ClientConnected()
        {
            if (_client == null)
            {
                // better to throw as there is a problem with creating the
                // instance rather than connecting to the IPFS daemon
                //
                throw new ArgumentNullException(); 
            }

            try
            {
                // Try to get id of this peer. If the daemon is not running than
                // it will throw a socket connection exception. This is just to make
                // sure that the environment is set-up correctly
                //
                var x = _client.DoCommandAsync("id", default(CancellationToken)).Result;

                // It will throw if the json object is not well formed.
                //
                var j = JObject.Parse(x);

                // Just to give an hint that the peer is up and running and has an ID
                //
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
           //
           throw new SocketException();
        }
        
        /// <summary>
        ///   Default constructor for IpfsConnector. It will attempt to to connect to the daemon
        ///   It will throw an exception if it cannot connect.
        /// </summary>
        public IpfsConnector()
        {
            TryToConnectClient();
        }
        
        public async Task<Ipfs.Cid> AddTextAsync(string text)
        {
            try
            {
                var fsn = await _client.FileSystem.AddTextAsync(text);
                Console.WriteLine(fsn.DataStream.ToString());
                return fsn.Id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public async Task<Ipfs.Cid> AddFileAsync(string filename)
        {
            try
            {
                var fsn = await _client.FileSystem.AddFileAsync(filename);
                return fsn.Id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<string> ReadAllTextAsync(string filename)
        {
            try
            {
                var text = await _client.FileSystem.ReadAllTextAsync(filename);
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
