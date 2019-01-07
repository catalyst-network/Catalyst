using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ADL.Hex.HexConverters.Extensions;
using ADL.RLP;
using ADL.KeySigner.Crypto;

namespace ADL.KeySigner
{
#if !DOTNET35
    public abstract class AtlasExternalSignerBase : IAtlasExternalSigner
    {
        protected abstract Task<byte[]> GetPublicKeyAsync();
        protected abstract Task<ECDSASignature> SignExternallyAsync(byte[] bytes);
        public abstract Task SignAsync(TransactionChainId transaction);
        public abstract Task SignAsync(Transaction transaction);
        public abstract ExternalSignerTransactionFormat ExternalSignerTransactionFormat { get; protected set; }
        public abstract bool CalculatesV { get; protected set; }

        public virtual async Task<string> GetAddressAsync()
        {
            var publicKey = await GetPublicKeyAsync();
            return new AtlasECKey(publicKey, false).GetPublicAddress();
        }

        public async Task<AtlasECDSASignature> SignAsync(byte[] rawBytes, BigInteger chainId)
        {
            var signature = await SignExternallyAsync(rawBytes);
            if (CalculatesV) return new AtlasECDSASignature(signature);

            var publicKey = await GetPublicKeyAsync();
            var recId = AtlasECKey.CalculateRecId(signature, rawBytes, publicKey);
            var vChain = AtlasECKey.CalculateV(chainId, recId);
            signature.V = vChain.ToBytesForRlpEncoding();
            return new AtlasECDSASignature(signature);
        }

        public async Task<AtlasECDSASignature> SignAsync(byte[] rawBytes)
        {
            var signature = await SignExternallyAsync(rawBytes);
            if (CalculatesV) return new AtlasECDSASignature(signature);

            var publicKey = await GetPublicKeyAsync();
            var recId = AtlasECKey.CalculateRecId(signature, rawBytes, publicKey);
            signature.V = new[] {(byte) (recId + 27)};
            return new AtlasECDSASignature(signature);
        }

        protected async Task SignHashTransactionAsync(Transaction transaction)
        {
            if (ExternalSignerTransactionFormat == ExternalSignerTransactionFormat.Hash)
            {
                var signature = await SignAsync(transaction.RawHash);
                transaction.SetSignature(signature);
            }
        }

        protected async Task SignRLPTransactionAsync(Transaction transaction)
        {
            if (ExternalSignerTransactionFormat == ExternalSignerTransactionFormat.RLP)
            {
                var signature = await SignAsync(transaction.GetRLPEncodedRaw());
                transaction.SetSignature(signature);
            }
        }

        protected async Task SignHashTransactionAsync(TransactionChainId transaction)
        {
            if (ExternalSignerTransactionFormat == ExternalSignerTransactionFormat.Hash)
            {
                var signature = await SignAsync(transaction.RawHash, transaction.GetChainIdAsBigInteger());
                transaction.SetSignature(signature);
            }
        }

        protected async Task SignRLPTransactionAsync(TransactionChainId transaction)
        {
            if (ExternalSignerTransactionFormat == ExternalSignerTransactionFormat.RLP)
            {
                var signature = await SignAsync(transaction.RawHash, transaction.GetChainIdAsBigInteger());
                transaction.SetSignature(signature);
            }
        }
    }
#endif
}