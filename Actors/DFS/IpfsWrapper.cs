using System.Threading.Tasks;
using System;
using Ipfs.Api;

namespace ADL.DFS
{
    public class IpfsWrapper
    {
        private readonly IpfsClient Client = new IpfsClient();

        public async Task<Ipfs.Cid> AddTextAsync(string text)
        {
            try
            {
                var fsn = await Client.FileSystem.AddTextAsync(text);
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
                var fsn = await Client.FileSystem.AddFileAsync(filename);
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
                var text = await Client.FileSystem.ReadAllTextAsync(filename);
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