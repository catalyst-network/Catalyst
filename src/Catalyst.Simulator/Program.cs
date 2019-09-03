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
using System.Linq;
using Catalyst.Core.Cli;
using Catalyst.Core.Cryptography;
using Catalyst.Core.FileSystem;
using Catalyst.Core.Keystore;
using Catalyst.Protocol.Common;
using Catalyst.Simulator.Extensions;
using Catalyst.Simulator.Helpers;
using Catalyst.Simulator.Simulations;
using CommandLine;
using Serilog;

namespace Catalyst.Simulator
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            var userOutput = new ConsoleUserOutput();
            userOutput.WriteLine("Catalyst Network Simulator");

            var passwordRegistry = new PasswordRegistry();
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => passwordRegistry.SetFromOptions(options));

            var fileSystem = new FileSystem();
            var userInput = new ConsoleUserInput();
            var consolePasswordReader = new ConsolePasswordReader(userOutput, userInput);
            var certificateStore = new CertificateStore(fileSystem, new PasswordManager(consolePasswordReader, passwordRegistry));
            var certificate = certificateStore.ReadOrCreateCertificateFile("mycert.pfx");
            var signingContext = new SigningContextProvider {Network = Network.Devnet, SignatureType = SignatureType.ProtocolPeer};

            var clientRpcInfoList =
                ConfigHelper.GenerateClientRpcInfoFromConfig(userOutput, passwordRegistry, certificate, logger, signingContext
                ).ToList();

            var simulation = new TransactionSimulation(userOutput);
            var simulator = new Simulator(simulation, logger);
            simulator.SimulateAsync(clientRpcInfoList).Wait();

            return Environment.ExitCode;
        }
    }
}
