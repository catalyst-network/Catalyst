using System.Threading.Tasks;
using System;
using Ipfs.Api;

namespace ADL.DFS
{
    public interface IDFS
    {
        Task<Ipfs.Cid> AddTextAsync(string text);
        Task<Ipfs.Cid> AddFileAsync(string filename);
        Task<string> ReadAllTextAsync(string filename);
    }
}