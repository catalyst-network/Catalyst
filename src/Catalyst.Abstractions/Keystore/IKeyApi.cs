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

using System.Collections.Generic;
using System.Security;
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

        Task<IKey> ImportAsync(string name, string pem, char[] password, CancellationToken cancel = default);
        
        Task<IKey> GetPublicKeyAsync(string self);
        Task<AsymmetricKeyParameter> GetPrivateKeyAsync(string self);
        
        Task<byte[]> CreateProtectedDataAsync(string keyName,
            byte[] plainText,
            CancellationToken cancel = default(CancellationToken));

        Task<byte[]> ReadProtectedDataAsync(byte[] cipherText,
            CancellationToken cancel = default(CancellationToken));

        Task SetPassphraseAsync(SecureString passphrase,
            CancellationToken cancel = default);
    }
}
