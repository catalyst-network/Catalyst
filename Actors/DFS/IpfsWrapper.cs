using System.Threading.Tasks;
using System;
using Ipfs.Api;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace ADL.DFS
{
    public class IpfsWrapper
    {
        private readonly IpfsClient _client = new IpfsClient();

        public IpfsWrapper()
        {
            if (_client == null)
            {
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
                Console.WriteLine("IPFS peer ID = " + (j["ID"] != null ? $"{j["ID"]}" : "field not found"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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