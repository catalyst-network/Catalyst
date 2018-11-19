using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Ipfs.Api;

namespace ADL.Dfs.Tests
{        
    [TestClass]
    public class FileSystemTest
    {
        private IpfsWrapper _ipfs = TestFixture._ipfs_wrapper;
        
        [TestMethod]
        public void AddFileAsync()
        {
            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, "hello my friends");
                    
            var task = _ipfs.AddFileAsync(tmpFile);
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", task.Result.Encode());
        }
        
        [TestMethod]
        public void AddTextAsync()
        {
            var hash = _ipfs.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", hash.Result.Hash);
        }
        
        [TestMethod]
        public void ReadAllTextAsync()
        {
            var hash = _ipfs.AddTextAsync("hello world");
                        
            var text = _ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD");
            Assert.AreEqual("hello world", text.Result);
        }
    }
}