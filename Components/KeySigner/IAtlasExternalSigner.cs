using System.Threading.Tasks;
using ADL.KeySigner.Crypto;

namespace ADL.KeySigner
{
#if !DOTNET35

    public enum ExternalSignerTransactionFormat
    {
        RLP,
        Hash,
        Transaction
    }
    
#endif
}