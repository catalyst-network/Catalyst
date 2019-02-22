//using System;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Catalyst.Node.Common;
//using Catalyst.Node.Core.Modules.Dfs;
//using FluentAssertions;
//using Microsoft.CSharp.RuntimeBinder;
//using Newtonsoft.Json;
//using NSubstitute;
//using Xunit;

//namespace Catalyst.Node.Core.UnitTest.Modules.Dfs
//{
//    public class IpfsDfsTests
//    {
//        //private static readonly IpfsConnector Ipfs = new IpfsConnector();
//        private static IpfsDfs.ISettings _settings;

//        private static readonly Random Random;
//        private IIpfs _ipfsConnector;
//        private IpfsDfs _ipfsDfs;

//        private static string RandomString(int length)
//        {
//            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
//            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
//        }

//        static IpfsDfsTests()
//        {
//            Random = new Random();
//        }

//        public IpfsDfsTests()
//        {
//            _settings = Substitute.For<IpfsDfs.ISettings>();
//            _ipfsConnector = Substitute.For<IIpfs>();

//            _ipfsDfs = new IpfsDfs(_ipfsConnector, _settings);
//        }

//        [Fact]
//        public void Start_should_use_correct_settings()
//        {
//            var connectRetries = (ushort)123;
//            _settings.ConnectRetries.Returns(connectRetries);
//            var versionAlpha = "version 12.alpha";
//            _settings.IpfsVersionApi.Returns(versionAlpha);

//            _ipfsDfs.Start();

//            _ipfsConnector.Received(1).CreateIpfsClient(versionAlpha, connectRetries);
//        }

//        [Fact]
//        public void Dispose_should_destroy_underlying_client()
//        {
//            _ipfsDfs.Dispose();
//            _ipfsConnector.Received(1).DestroyIpfsClient();
//        }

//        [Fact]
//        public void AddFile_should_call_underlying_client()
//        {
//            var filename = RandomString(23);
//            _ipfsDfs.AddFile(filename);
//            _ipfsConnector.Received(1).AddFile(filename);
//        }


//        [Fact]
//        public async Task ReadAllTextAsync_should_call_underlying_client()
//        {
//            var filename = RandomString(23);
//            await _ipfsDfs.ReadAllTextAsync(filename);
//            await _ipfsConnector.Received(1).ReadAllTextAsync(filename);
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void AddFile()
//        {
//            var tmpFile = Path.GetTempFileName();
//            File.WriteAllText(tmpFile, "hello my friends");

//            var hash = Ipfs.AddFile(tmpFile);
//            hash.Should().Be("QmaMjZpjD17yRfCwk6Yg8aRnspyR4EcvCsqoyBECCP8bjJ");
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void AddFileAsync_EmptyFilename()
//        {
//            new Action(() => Ipfs.AddFile("")).Should().Throw<JsonReaderException>();
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void ReadAllTextAsync()
//        {
//            var text = Ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD");
//            text.Result.Should().Be("hello world");
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void ReadAllTextAsync_Dodgy_Hash()
//        {
            
//            new Action(() =>{
//                    var result = Ipfs.ReadAllTextAsync("Qmf412jQZiuVAbcdefghilmnopqrst12").Result;
//                }).Should().Throw<AggregateException>("invalid ipfs ref path");
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void StopDoesNotThrow()
//        {
//            Ipfs.IsClientConnected().Should().BeTrue();

//            new Action(() => Ipfs.DestroyIpfsClient())
//                .Should().NotThrow();
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void Stop_Start_DoesNotThrow()
//        {
//            new Action(() =>
//            {
//                Ipfs.DestroyIpfsClient();
//                Ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
//            }).Should().NotThrow();

//            Ipfs.IsClientConnected().Should().BeTrue();
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void Stop_Add_Fail()
//        {
//            new Action(() =>
//            {
//                Ipfs.DestroyIpfsClient();

//                var tmpFile = Path.GetTempFileName();
//                Ipfs.AddFile(tmpFile);
//            }).Should().Throw<RuntimeBinderException>("Cannot perform runtime binding on a null reference");

//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void Stop_Read_Fail()
//        {
//            new Action(() =>
//            {
//                Ipfs.DestroyIpfsClient();
//                var text = Ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD").Result; 
//            }).Should().Throw<AggregateException>("Connection refused");
           
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void Stop_Twice_NoThrow()
//        {
//            new Action(() => {
//                Ipfs.DestroyIpfsClient();
//                Ipfs.DestroyIpfsClient();
//            }).Should().NotThrow();
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void Start_Twice_NoThrow()
//        {
//            new Action(() => {
//                Ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
//                Ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
//            }).Should().NotThrow();
//            Ipfs.IsClientConnected().Should().BeTrue();
//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void ChangeSettings_FailToStart()
//        {
//            new Action(() =>
//            {
//                //var x = new TestDfsSettings {StorageType = "Ipfs", ConnectRetries = 0, IpfsVersionApi = "api/v0/"};
//                Ipfs.DestroyIpfsClient();
//                //Ipfs.CreateIpfsClient(x.IpfsVersionApi, x.ConnectRetries);
//            }).Should().Throw<InvalidOperationException>("Failed to connect with IPFS daemon");

//        }

//        [Fact(Skip="We will probably use a native .Net IPFS client")]
//        public void SequentialAdd()
//        {
//            const int numIter = 50;

//            for (var i = 0; i < numIter; i++)
//            {
//                var tmpFile = Path.GetTempFileName();
//                File.WriteAllText(tmpFile, RandomString(32));

//                var hash1 = Ipfs.AddFile(tmpFile);
//                hash1.Length.Should().Be(46); // just make sure was added
//                var hash2 = Ipfs.AddFile(tmpFile); // retrieve the hash back, does not add
//                hash1.Should().Be(hash2);
//            }
//        }
//    }
//}