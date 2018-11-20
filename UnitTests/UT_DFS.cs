using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADL.DFS
{
    [TestClass]
    public class FileSystemTest
    {
        private IpfsConnector _ipfs = new IpfsConnector();
        
        [TestMethod]
        public void AddFileAsync()
        {
            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, "hello my friends");
                    
            var hash = _ipfs.AddFile(tmpFile);
            Assert.AreEqual("QmaMjZpjD17yRfCwk6Yg8aRnspyR4EcvCsqoyBECCP8bjJ", hash);
        }
        
        [TestMethod]
        [ExpectedException(typeof(Newtonsoft.Json.JsonReaderException))]
        public void AddFileAsync_EmptyFilename()
        {                    
            _ipfs.AddFile("");
        }
        
        [TestMethod]
        public void ReadAllTextAsync()
        {   
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
