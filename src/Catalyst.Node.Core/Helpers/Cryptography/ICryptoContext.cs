using System;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public interface ICryptoContext{
        
        /// <summary>
        /// Generates a wrapped private key using underlying crypto context.
        /// </summary>
        /// <returns></returns>
        IPrivateKey GeneratePrivateKey();
        
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
        
        /// <summary>
        /// Creates wrapped private key from keyblob. Returns null if failed.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        IPrivateKey ImportPrivateKey(ReadOnlySpan<byte> blob);
        
        /// <summary>
        /// Creates keyblob from private key. Returns null if failed.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        byte[] ExportPrivateKey(IPrivateKey key);
        
        /// <summary>
        /// Creates signature using data and provided private key. 
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] Sign(IPrivateKey privateKey, ReadOnlySpan<byte> data);

        bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);

        /// <summary>
        /// Given a private key returns corresponding public key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IPublicKey GetPublicKey(IPrivateKey key);
    }
}