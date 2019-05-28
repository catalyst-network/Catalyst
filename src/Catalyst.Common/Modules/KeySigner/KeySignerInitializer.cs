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
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Nethereum.KeyStore.Crypto;
using Serilog;

namespace Catalyst.Common.Modules.KeySigner
{
    public class KeySignerInitializer : IKeySignerInitializer
    {
        private static int MaxTries => 5;

        private readonly ILogger _logger;

        private readonly IPasswordReader _passwordReader;

        private SecureString _password;

        protected readonly IKeyStore KeyStore;

        public KeySignerInitializer(IPasswordReader passwordReader, IKeyStore keyStore, ILogger logger)
        {
            KeyStore = keyStore;
            _passwordReader = passwordReader;
            _logger = logger;
        }

        public string Password
        {
            get
            {
                if (_password == null)
                {
                    return string.Empty;
                }

                IntPtr stringPointer = Marshal.SecureStringToBSTR(_password);
                string normalString = Marshal.PtrToStringBSTR(stringPointer);
                Marshal.ZeroFreeBSTR(stringPointer);
                return normalString;
            }
        }

        public void ReadPassword(IKeySigner keySigner)
        {
            int tries = 0;
            bool success = false;

            while (!success && tries < MaxTries)
            {
                _password = _passwordReader.ReadSecurePassword("Please enter key signer password");

                try
                {
                    Initialize(keySigner);
                    success = true;
                }
                catch (DecryptionException)
                {
                    _logger.Error("Error decrypting key signer IKeySignerInitializer.ReadPassword");
                }

                tries += 1;
            }

            if (!success)
            {
                throw new InvalidOperationException("Failed to decrypt key signer, exiting");
            }
        }

        private void Initialize(IKeySigner keySigner)
        {
            var path = Path.Combine(KeyStore.GetBaseDir(), Constants.DefaultKeyStoreFile);
            var keyStoreFileExists = File.Exists(path);

            try
            {
                if (keyStoreFileExists)
                { 
                    KeyStore.GetKey(Constants.DefaultKeyStoreFile, Password).GetPublicKey();
                }
                else
                {
                    keySigner.GenerateNewKey();
                }
            }
            catch (DecryptionException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error on IKeySignerInitializer.Initialize");
            }
        }
    }
}
