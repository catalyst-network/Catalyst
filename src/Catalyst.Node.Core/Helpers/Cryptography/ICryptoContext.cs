using System;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public interface ICryptoContext{
        
        /// <summary>
        /// Generates a wrapped private key using underlying crypto context.
        /// </summary>
        /// <returns></returns>
        IKey GenerateKey();
        
        /// <summary>
        /// Creates wrapped public key from keyblob. Returns null if failed.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob);
        
        /// <summary>
        /// Creates keyblob from public key. Returns null if failed.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        byte[] ExportPublicKey(IPublicKey key);
        
        byte[] Sign(IKey key, ReadOnlySpan<byte> data);

        bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);
    }
}