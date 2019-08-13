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
using System.Security;
using System.Threading.Tasks;
using Catalyst.Common.Registry;
using Catalyst.Common.Types;
using CommandLine;

namespace Catalyst.Simulator
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Catalyst Network Simulator");

            var passwordRegistry = new PasswordRegistry();
            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                if (!string.IsNullOrEmpty(options.NodePassword))
                {
                    AddPassword(passwordRegistry, options.NodePassword);
                }

                if (!string.IsNullOrEmpty(options.SslCertPassword))
                {
                    AddPassword(passwordRegistry, options.SslCertPassword);
                }
            });

            var simulator = new Simulator(passwordRegistry);
            await simulator.Simulate();

            return Environment.ExitCode;
        }

        private static void AddPassword(PasswordRegistry passwordRegistry, string password)
        {
            var secureString = new SecureString();
            foreach (var character in password)
            {
                secureString.AppendChar(character);
            }

            passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.CertificatePassword, secureString);
        }
    }
}
