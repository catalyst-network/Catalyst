using System;
using System.IO;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ADL.Ipfs;
using ADL.Node.Core.Modules.Dfs;

namespace ADL.UnitTests
{
    [TestClass]
    public class PeerServerTest
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
            _ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
        }
        
        [TestInitialize]
        public void Initialize()
        {
            _ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
        }
        
        [TestMethod]
        public void AddFile()
        {
            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, "hello my friends");

            var hash = _ipfs.AddFile(tmpFile);
            Assert.AreEqual("QmaMjZpjD17yRfCwk6Yg8aRnspyR4EcvCsqoyBECCP8bjJ", hash);
        }
    }
}
