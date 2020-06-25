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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Core.Lib.Config;
using Catalyst.NetworkUtils;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;


namespace Catalyst.Node.POA.CE.Tests.UnitTests.Config
{
    
    public sealed class PoaConfigEditorTests : FileSystemBasedTest
    {
        private readonly IAddressProvider _addressProvider = Substitute.For<IAddressProvider>();
        private sealed class ConfigFilesEditTestData : List<object[]>
        {
            public ConfigFilesEditTestData()
            {
                Add(new object[] { Constants.NetworkConfigFile(NetworkType.Mainnet), NetworkType.Mainnet });
                Add(new object[] { Constants.NetworkConfigFile(NetworkType.Testnet), NetworkType.Testnet });
                Add(new object[] { Constants.NetworkConfigFile(NetworkType.Devnet), NetworkType.Devnet });
            }
        }

        [SetUp]
        public void Init()
        {
            Setup(TestContext.CurrentContext);
            _addressProvider.GetLocalIpAsync().ReturnsForAnyArgs(IPAddress.Parse("192.168.0.12"));
            _addressProvider.GetPublicIpAsync().ReturnsForAnyArgs(IPAddress.Parse("86.13.185.24"));
        }

        [TestCaseSource(typeof(ConfigFilesEditTestData))]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public async Task RunConfigEditor_Should_Overwrite_Specified_Values_For_Each_Network_File(string moduleFileName,
            NetworkType network)
        {
            await RunConfigEditor_Should_Overwrite_Specified_Values(moduleFileName, network);
        }

        private async Task RunConfigEditor_Should_Overwrite_Specified_Values(string fileName, NetworkType network = NetworkType.Devnet)
        {
            var currentDirectory = FileSystem.GetCatalystDataDir();

            var networkFileText =
                await File.ReadAllTextAsync(Path.Combine(Constants.ConfigSubFolder, fileName))
                    .ConfigureAwait(false);
            
            await FileSystem.WriteTextFileToCddAsync(fileName,networkFileText)
                .ConfigureAwait(false);
            
            new PoaConfigEditor(_addressProvider).RunConfigEditor(currentDirectory.FullName, network);
            
            var editedNetworkFileText = await File.ReadAllTextAsync(Path.Combine(currentDirectory.FullName, fileName));

            editedNetworkFileText.Should().NotBe(networkFileText);
        }
    }
}
