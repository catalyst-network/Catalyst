#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using System;
using System.Security;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.Cryptography
{
    /// <summary>
    /// Node password repeater.
    /// This class will continuously prompt the user to input their password until the password is correct.
    /// </summary>
    public class NodePasswordRepeater : IPasswordRepeater
    {
        private readonly IKeyApi _keyApi;
        private readonly IPasswordManager _passwordManager;
        private readonly IUserOutput _userOutput;

        public NodePasswordRepeater(IKeyApi keyApi, IPasswordManager passwordManager, IUserOutput userOutput)
        {
            _keyApi = keyApi;
            _passwordManager = passwordManager;
            _userOutput = userOutput;
        }

        /// <inheritdoc />
        public async Task<SecureString> PromptAndReceiveAsync()
        {
            while (true)
            {
                try
                {
                    var password = _passwordManager.PromptPassword(PasswordRegistryTypes.DefaultNodePassword, "Please provide your node password");
                    await _keyApi.SetPassphraseAsync(password).ConfigureAwait(false);
                    return password;
                }
                catch (UnauthorizedAccessException)
                {
                    _userOutput.WriteLine($"Invalid node password, please try again.");
                }
            }
        }

        /// <inheritdoc />
        public async Task PromptAndAddPasswordToRegistryAsync()
        {
            var password = await PromptAndReceiveAsync().ConfigureAwait(false);
            _passwordManager.AddPasswordToRegistry(PasswordRegistryTypes.DefaultNodePassword, password);
        }
    }
}
