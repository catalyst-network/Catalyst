using Catalyst.Node.Common.Interfaces;
using Serilog;

namespace Catalyst.Node.Common.Components
{
    public class LocalKeyStore : IKeyStore
    {
        private readonly ILogger _logger;
        public ICryptoContext CryptoContext { get; }

        public LocalKeyStore(ICryptoContext cryptoContext, ILogger logger)
        {
            CryptoContext = cryptoContext;
            _logger = logger; 
            _logger.Information("Im a Keystore");
        }

        public void GetKey() { throw new System.NotImplementedException(); }
        public void StoreKey() { throw new System.NotImplementedException(); }
    }
}
