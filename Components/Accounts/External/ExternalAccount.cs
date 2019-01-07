using System.Numerics;
using System.Threading.Tasks;
using ADL.JsonRpc.Client;
using ADL.RPC.Accounts;
using ADL.RPC.NonceServices;
using ADL.RPC.TransactionManagers;
using ADL.Signer;

namespace ADL.Accounts
{
    public class ExternalAccount : IAccount
    {
        public IEthExternalSigner ExternalSigner { get; }
        public BigInteger? ChainId { get; }


        public ExternalAccount(IEthExternalSigner externalSigner, BigInteger? chainId = null)
        {
            ExternalSigner = externalSigner;
            ChainId = chainId;
        }

        public ExternalAccount(string address, IEthExternalSigner externalSigner, BigInteger? chainId = null)
        {
            ChainId = chainId;
            Address = address;
            ExternalSigner = externalSigner;
        }

        public async Task InitialiseAsync()
        {
            Address = await ExternalSigner.GetAddressAsync();
        }

        public void InitialiseDefaultTransactionManager(IClient client)
        {
            TransactionManager = new ExternalAccountSignerTransactionManager(client, this, ChainId);
        }

        public string Address { get; protected set; }
        public ITransactionManager TransactionManager { get; protected set; }
        public INonceService NonceService { get; set; }
    }
}