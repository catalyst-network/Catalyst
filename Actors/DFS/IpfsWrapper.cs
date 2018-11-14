using System.Threading.Tasks;
using System;
using System.Threading;
using Ipfs.Api;
using Ipfs.CoreApi;

namespace ADL.DFS
{
    public class IpfsWrapper
    {
        private static readonly IpfsClient Client = new IpfsClient();
        
        public async Task<string> AddFileAsync(string path)
        {
            //var fsn = await Client.FileSystem.AddFileAsync(path);
            var options = new AddFileOptions { OnlyHash = true };
            var fsn = await Client.FileSystem.AddTextAsync("hello world", options);
            Console.WriteLine(fsn.Id);
            return fsn.Id;
        }

        public async Task<string> ReadFileAsync(string hash)
        {
            const string filename = "/ipfs/QmYwAPJzv5CZsnA625s3Xf2nemtYgPpHdWEz79ojWnPbdG/readme";
            var text = await Client.FileSystem.ReadAllTextAsync(filename);
            Thread.Sleep(10000);
            Console.WriteLine($"message returned:{text}");
            //using (var stream = await Client.FileSystem.ReadFileAsync(hash))
            //{
                // Process stream
            //}

            //return true;

            return text;
        }
    }
}