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

using System.IO;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.Tests.IntegrationTests.Config
{
    public sealed class ConfigurableRepoIntegrationTests : FileSystemBasedTest
    {
        public ConfigurableRepoIntegrationTests(ITestOutputHelper output) : base(output) { }

        private async Task ModuleCanSaveAndRetrieveValuesFromRepository(FileInfo moduleFile)
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName + moduleFile))
            {
                var component = scope.Resolve<IComponentWithRepository>();

                var guid = CorrelationId.GenerateCorrelationId().ToString();
                var storedItem = new StoredItem {Name = guid, Value = 10};

                component.StringRepository.Add(storedItem);

                component.StringRepository.TryFind(s => s.Name == guid, out var retrieveItem);

                retrieveItem.Name.Should().Be(storedItem.Name);
                retrieveItem.Value.Should().Be(storedItem.Value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { }
        }
    }
}
