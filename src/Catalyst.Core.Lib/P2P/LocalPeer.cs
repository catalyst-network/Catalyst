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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Abstractions.Types;
using System.Reflection;
using LibP2P = Lib.P2P;

namespace Catalyst.Core.Lib.P2P
{
    public class LocalPeer : LibP2P.Peer
    {
        public LocalPeer(IKeyApi keyApi, KeyChainOptions keyChainOptions, IPasswordManager passwordManager)
        {
            var passphrase = passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.DefaultNodePassword, "Please provide your node password");
            keyApi.SetPassphraseAsync(passphrase).ConfigureAwait(false).GetAwaiter().GetResult();

            var self = keyApi.GetKeyAsync("self").ConfigureAwait(false).GetAwaiter().GetResult() ??
                     keyApi.CreateAsync("self", keyChainOptions.DefaultKeyType, 0).ConfigureAwait(false).GetAwaiter().GetResult();

            this.Id = self.Id;
            this.PublicKey = keyApi.GetDfsPublicKeyAsync("self").ConfigureAwait(false).GetAwaiter().GetResult();
            this.ProtocolVersion = "ipfs/0.1.0";

            var version = typeof(IDfsService).GetTypeInfo().Assembly.GetName().Version;
            this.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";
        }
    }
}
