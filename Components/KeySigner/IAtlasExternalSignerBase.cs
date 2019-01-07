using System.Numerics;
using System.Threading.Tasks;

namespace ADL.KeySigner
{
#if !DOTNET35
    public interface IAtlasExternalSigner
    {
        bool CalculatesV { get; }
        ExternalSignerTransactionFormat ExternalSignerTransactionFormat { get; }
        Task<string> GetAddressAsync();
        Task<AtlasECDSASignature> SignAsync(byte[] rawBytes);
        Task<AtlasECDSASignature> SignAsync(byte[] rawBytes, BigInteger chainId);
        Task SignAsync(Transaction transaction);
        Task SignAsync(TransactionChainId transaction);
    }
#endif
}