namespace ADL.DFS
{
    public interface IDFSService
    {
        public async Task<Ipfs.Cid> AddTextAsync(string text);
        public async Task<Ipfs.Cid> AddFileAsync(string filename);
        public async Task<string> ReadAllTextAsync(string filename);
    }
}