using System.Threading.Tasks;
using System;
using Ipfs.Api;

namespace ADL.DFS
{
    public class IpfsWrapper : IDfs
    {
        private readonly IpfsClient _client = new IpfsClient();

        public async Task<Ipfs.Cid> AddTextAsync(string text)
        {
            try
            {
                var fsn = await _client.FileSystem.AddTextAsync(text);
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