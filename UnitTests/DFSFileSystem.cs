using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADL.DFS
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
            var hash = TestFixture._ipfs_wrapper.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", hash.Result.Encode());
        }
        
        [TestMethod]
        public void ReadAllTextAsync()
        {
            var hash = _ipfs.AddTextAsync("hello world");
            
            var text = _ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD");
            Assert.AreEqual("hello world", text.Result);
        }
        
        [TestMethod]
        [ExpectedException(typeof(AggregateException), "invalid ipfs ref path")]
        public void ReadAllTextAsync_Dodgy_Hash()
        {
            var text = _ipfs.ReadAllTextAsync("Qmf412jQZiuVAbcdefghilmnopqrst12").Result;
        }
    }
}