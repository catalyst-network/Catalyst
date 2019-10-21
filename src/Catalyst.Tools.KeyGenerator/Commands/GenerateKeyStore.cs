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

using Catalyst.Tools.KeyGenerator.Options;
using System;
using System.IO;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Tools.KeyGenerator.Interfaces;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Network;

namespace Catalyst.Tools.KeyGenerator.Commands
{
    public class GenerateKeyStore : IKeyGeneratorCommand
    {
        private readonly IFileSystem _fileSystem;
        private readonly IKeyStore _keyStore;
        private readonly IUserOutput _userOutput;
        private readonly IPasswordRegistryLoader _passwordLoader;

        public GenerateKeyStore(IFileSystem fileSystem, IKeyStore keyStore, IUserOutput userOutput, IPasswordRegistryLoader passwordLoader)
        {
            _fileSystem = fileSystem;
            _keyStore = keyStore;
            _userOutput = userOutput;
            _passwordLoader = passwordLoader;
        }

        // Parses raw output
        public bool Parse(string[] args)
        {
            throw new NotSupportedException();
        }

        public void ParseOption(NetworkType networkType, object option)
        {
            var generateKeyStoreOption = (GenerateKeyStoreOption) option;

            if (!_fileSystem.SetCurrentPath(generateKeyStoreOption.Path))
            {
                _userOutput.WriteLine("Invalid path.");
                return;
            }

            var secureStr = _passwordLoader.PreloadPassword(generateKeyStoreOption.Password);
            Exception error = null;

            try
            {
                var privateKey = _keyStore.KeyStoreGenerate(networkType, KeyRegistryTypes.DefaultKey).ConfigureAwait(false)
                   .GetAwaiter().GetResult();
                var publicKey = privateKey.GetPublicKey().Bytes.KeyToString();

                _userOutput.WriteLine($"Generated key store at path: {Path.GetFullPath(generateKeyStoreOption.Path)}");
                _userOutput.WriteLine($"Public Key: {publicKey}");
            }
            catch (Exception e)
            {
                _userOutput.WriteLine($"Error generating keystore at path {generateKeyStoreOption.Path}");
                error = e;
            }
            finally
            {
                secureStr?.Dispose();
                if (error != null)
                {
                    throw error;
                }
            }
        }

        public string CommandName => "generate";
        public Type OptionType => typeof(GenerateKeyStoreOption);
    }
}
