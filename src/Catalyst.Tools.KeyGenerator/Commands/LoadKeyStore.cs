#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Util;
using Catalyst.Tools.KeyGenerator.Interfaces;
using Catalyst.Tools.KeyGenerator.Options;
using System;
using Catalyst.Protocol.Network;

namespace Catalyst.Tools.KeyGenerator.Commands
{
    public class LoadKeyStore : IKeyGeneratorCommand
    {
        private readonly IFileSystem _fileSystem;
        private readonly IKeyStore _keyStore;
        private readonly IUserOutput _userOutput;
        private readonly IPasswordRegistryLoader _passwordLoader;

        public LoadKeyStore(IFileSystem fileSystem, IKeyStore keyStore, IUserOutput userOutput, IPasswordRegistryLoader passwordLoader)
        {
            _fileSystem = fileSystem;
            _keyStore = keyStore;
            _userOutput = userOutput;
            _passwordLoader = passwordLoader;
        }

        public bool Parse(string[] args) { throw new NotImplementedException(); }
        public string CommandName => "load";
        public Type OptionType => typeof(LoadKeyStoreOption);

        public void ParseOption(NetworkType networkType, object option)
        {
            var loadKeyStoreOptions = (LoadKeyStoreOption) option;

            if (!_fileSystem.SetCurrentPath(loadKeyStoreOptions.Path))
            {
                _userOutput.WriteLine("Invalid path.");
                return;
            }

            var secureStr = _passwordLoader.PreloadPassword(loadKeyStoreOptions.Password);
            var privateKey = _keyStore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);

            // If no key store exists then KeyStoreDecrypt will log a message and return null for the private key
            if (privateKey == null)
            {
                secureStr?.Dispose();
                return;
            }

            var publicKeyStr = privateKey.GetPublicKey().Bytes.KeyToString();
            _userOutput.WriteLine($"Keystore decrypted, Public Key: {publicKeyStr}");
            secureStr?.Dispose();
        }
    }
}
