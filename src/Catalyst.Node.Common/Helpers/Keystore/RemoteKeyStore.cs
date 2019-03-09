using Catalyst.Node.Common.Interfaces;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class RemoteKeyStore : IKeyStore
    {
        private readonly ILogger _logger;
        private  ICryptoContext CryptoContext { get; }

        public RemoteKeyStore(ICryptoContext cryptoContext, ILogger logger)
        {
            CryptoContext = cryptoContext;
            _logger = logger; 
            _logger.Information("Im a remote Keystore");
        }

        public IPrivateKey GetKey(IPublicKey publicKey, string password) { throw new System.NotImplementedException(); }
        public IPrivateKey GetKey(string address, string password) { throw new System.NotImplementedException(); }
        public bool StoreKey(IPrivateKey privateKey, string address, string password) { throw new System.NotImplementedException(); }
    }
}
