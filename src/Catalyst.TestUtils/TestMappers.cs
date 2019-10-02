using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO;

namespace Catalyst.TestUtils
{
    public static class TestMappers
    {
        public static void Start()
        {
            var mappers = new IMapperInitializer[]
            {
                new ProtocolMessageDao(),
                new ConfidentialEntryDao(),
                new ProtocolErrorMessageSignedDao(),
                new PeerIdDao(),
                new SigningContextDao(),
                new CoinbaseEntryDao(),
                new PublicEntryDao(),
                new ConfidentialEntryDao(),
                new TransactionBroadcastDao(),
                new RangeProofDao(),
                new ContractEntryDao(),
                new SignatureDao(),
                new BaseEntryDao()
            };

            var map = new MapperProvider(mappers);
            map.Start();
        }
    }
}
