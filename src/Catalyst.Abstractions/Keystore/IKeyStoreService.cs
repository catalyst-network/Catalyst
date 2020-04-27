#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Options;
using Org.BouncyCastle.Crypto;

namespace Catalyst.Abstractions.Keystore
{
    public interface IKeyStoreService
    {
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
        Task<IKey> ImportAsync(string name, string pem, char[] password = null, CancellationToken cancel = default);
        
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
        Task<string> GetIpfsPublicKeyAsync(string name, CancellationToken cancel = default);

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
        ///   an <see cref="IKey"/> or <b>null</b> if the the key is not defined.
        /// </returns>
        Task<IKey> FindKeyByNameAsync(string name, CancellationToken cancel = default);

        /// <summary>
        ///   The configuration options.
        /// </summary>
        KeyChainOptions Options { get; set; }

        /// <summary>
        ///   Encrypt data as CMS protected data.
        /// </summary>
        /// <param name="keyName">
        ///   The key name to protect the <paramref name="plainText"/> with.
        /// </param>
        /// <param name="plainText">
        ///   The data to protect.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the cipher text of the <paramref name="plainText"/>.
        /// </returns>
        /// <remarks>
        ///   Cryptographic Message Syntax (CMS), aka PKCS #7 and 
        ///   <see href="https://tools.ietf.org/html/rfc5652">RFC 5652</see>,
        ///   describes an encapsulation syntax for data protection. It
        ///   is used to digitally sign, digest, authenticate, and/or encrypt
        ///   arbitrary message content.
        /// </remarks>
        Task<byte[]> CreateProtectedDataAsync(string keyName, byte[] plainText, CancellationToken cancel = default);

        /// <summary>
        ///   Decrypt CMS protected data.
        /// </summary>
        /// <param name="cipherText">
        ///   The protected CMS data.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the plain text byte array of the protected data.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///   When the required private key, to decrypt the data, is not foumd.
        /// </exception>
        /// <remarks>
        ///   Cryptographic Message Syntax (CMS), aka PKCS #7 and 
        ///   <see href="https://tools.ietf.org/html/rfc5652">RFC 5652</see>,
        ///   describes an encapsulation syntax for data protection. It
        ///   is used to digitally sign, digest, authenticate, and/or encrypt
        ///   arbitrary message content.
        /// </remarks>
        Task<byte[]> ReadProtectedDataAsync(byte[] cipherText, CancellationToken cancel = default);

        /// <summary>
        ///   Sets the passphrase for the key chain.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="UnauthorizedAccessException">
        ///   When the <paramref name="passphrase"/> is wrong.
        /// </exception>
        /// <remarks>
        ///   The <paramref name="passphrase"/> is used to generate a DEK (derived encryption
        ///   key).  The DEK is then used to encrypt the stored keys.
        ///   <para>
        ///   Neither the <paramref name="passphrase"/> nor the DEK are stored.
        ///   </para>
        /// </remarks>
        Task SetPassphraseAsync(SecureString passphrase, CancellationToken cancel = default);

        /// <inheritdoc />
        Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default);

        /// <inheritdoc />
        Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default);

        /// <inheritdoc />
        Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default);

        /// <inheritdoc />
        Task<IKey> RemoveAsync(string name, CancellationToken cancel = default);

        /// <inheritdoc />
        Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default);

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
        Task<AsymmetricKeyParameter> GetPrivateKeyAsync(string name, CancellationToken cancel = default);
        Task<AsymmetricKeyParameter> GetPublicKeyAsync(string name, CancellationToken cancel = default);

        /// <summary>
        ///   Create a X509 certificate for the specified key.
        /// </summary>
        /// <param name="keyName">
        ///   The key name.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<byte[]> CreateCertificateAsync(string keyName, CancellationToken cancel = default);
    }
}
