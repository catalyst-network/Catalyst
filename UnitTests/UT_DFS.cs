using System;
using System.IO;
using ADL.Node;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADL.Dfs
{
    [TestClass]
    public class UT_Dfs
    {
        private static IpfsConnector _ipfs = new IpfsConnector();

        private class TestDfsSettings : IDfsSettings // sort of mock
        {
            public string StorageType { get; set; }
            public ushort ConnectRetries { get; set; }
            public string IpfsVersionApi { get; set; }
        }

        private static TestDfsSettings _settings;
        
        [ClassInitialize]
        public static void SetUp(TestContext testContext)
        {
            _settings = new TestDfsSettings {StorageType = "Ipfs", ConnectRetries = 10, IpfsVersionApi = "api/v0/"};
            _ipfs.Start(_settings);
        }
        
        [TestInitialize]
        public void Initialize()
        {
            _ipfs.Start(_settings);
        }
        
        [TestMethod]
        public void AddFile()
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

        [TestMethod]
        public void StopDoesNotThrow()
        {
            Assert.IsTrue(_ipfs.IsClientConnected());
            
            try
            {
                _ipfs.Stop();
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }
        
        [TestMethod]
        public void Stop_Start_DoesNotThrow()
        {
            try
            {
                _ipfs.Stop();
                _ipfs.Start(_settings);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }

            Assert.IsTrue(_ipfs.IsClientConnected());
        }

        [TestMethod]
        [ExpectedException(typeof(RuntimeBinderException), "Cannot perform runtime binding on a null reference")]
        public void Stop_Add_Fail()
        {
            _ipfs.Stop();
            
            var tmpFile = Path.GetTempFileName();
            _ipfs.AddFile(tmpFile);
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException), "Connection refused")]
        public void Stop_Read_Fail()
        {
            _ipfs.Stop();
            var text = _ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD").Result;
        }
        
        [TestMethod]
        public void Stop_Twice_NoThrow()
        {
            try
            {
                _ipfs.Stop();
                _ipfs.Stop();
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }
        
        [TestMethod]
        public void Start_Twice_NoThrow()
        {
            try
            {
                _ipfs.Start(_settings);
                _ipfs.Start(_settings);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
                        
            Assert.IsTrue(_ipfs.IsClientConnected());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "Failed to connect with IPFS daemon")]
        public void ChangeSettings_FailToStart()
        {
            var x = new TestDfsSettings {StorageType = "Ipfs", ConnectRetries = 0, IpfsVersionApi = "api/v0/"};
            _ipfs.Stop();
            _ipfs.Start(x);
        }
    }
}
