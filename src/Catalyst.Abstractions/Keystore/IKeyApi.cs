using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Org.BouncyCastle.Crypto;

namespace Catalyst.Abstractions.Keystore
{
    /// <summary>
    ///   Manages cryptographic keys.
    /// </summary>
    /// <remarks>
    ///   <note>
    ///   The Key API is work in progress! There be dragons here.
    ///   </note>
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/KEY.md">Key API spec</seealso>
    public interface IKeyApi
    {
        /// <summary>
        ///   Creates a new key.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="keyType">
        ///   The type of key to create; "rsa" or "ed25519".
        /// </param>
        /// <param name="size">
        ///   The size, in bits, of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the key that was created.
        /// </returns>
        Task<IKey> CreateAsync(string name,
            string keyType,
            int size,
            CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   List all the keys.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a sequence of IPFS keys.
        /// </returns>
        Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Delete the specified key.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the key that was deleted.
        /// </returns>
        Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Rename the specified key.
        /// </summary>
        /// <param name="oldName">
        ///   The local name of the key.
        /// </param>
        /// <param name="newName">
        ///   The new local name of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a sequence of IPFS keys that were renamed.
        /// </returns>
        Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Export a key to a PEM encoded password protected PKCS #8 container.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="password">
        ///   The PEM's password.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous operation. The task's result is
        ///    the password protected PEM string.
        /// </returns>
        Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Import a key from a PEM encoded password protected PKCS #8 container.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="pem">
        ///   The PEM encoded PKCS #8 container.
        /// </param>
        /// <param name="password">
        ///   The <paramref name="pem"/>'s password.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous operation. The task's result
        ///    is the newly imported key.
        /// </returns>
        Task<IKey> ImportAsync(string name,
            string pem,
            char[] password = null,
            CancellationToken cancel = default(CancellationToken));
        
        /// <summary>
        ///   Gets the IPFS encoded public key for the specified key.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the IPFS encoded public key.
        /// </returns>
        /// <remarks>
        ///   The IPFS public key is the base-64 encoding of a protobuf encoding containing 
        ///   a type and the DER encoding of the PKCS Subject Public Key Info.
        /// </remarks>
        /// <seealso href="https://tools.ietf.org/html/rfc5280#section-4.1.2.7"/>
        Task<string> GetPublicKeyAsync(string name, CancellationToken cancel = default(CancellationToken));
        
        /// <summary>
        ///   Gets the Bouncy Castle representation of the private key.
        /// </summary>
        /// <param name="name">
        ///   The local name of key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the private key as an <b>AsymmetricKeyParameter</b>.
        /// </returns>
        Task<AsymmetricKeyParameter> GetPrivateKeyAsync(string name,
            CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Find a key by its name.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   an <see cref="Microsoft.EntityFrameworkCore.Metadata.IKey"/> or <b>null</b> if the the key is not defined.
        /// </returns>
        Task<IKey> FindKeyByNameAsync(string name, CancellationToken cancel = default(CancellationToken));

        Task<byte[]> CreateProtectedDataAsync(string keyName,
            byte[] plainText,
            CancellationToken cancel = default(CancellationToken));

        Task<byte[]> ReadProtectedDataAsync(byte[] cipherText,
            CancellationToken cancel = default(CancellationToken));
    }
}
