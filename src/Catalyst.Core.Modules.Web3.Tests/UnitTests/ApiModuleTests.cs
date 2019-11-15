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
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace Catalyst.Core.Modules.Web3.Client.Tests.UnitTests
{
    public sealed class ApiModuleTests
    {
        private ApiModule _apiModule;
        private readonly IServiceCollection _serviceCollection;

        public ApiModuleTests()
        {
            _serviceCollection = new ServiceCollection();
        }

        [Fact]
        public void Can_Add_Swagger() { AssertService(typeof(ISwaggerProvider)); }

        [Fact]
        public void Can_Not_Add_Swagger() { AssertService(typeof(ISwaggerProvider), false, false); }

        [Fact]
        public void Can_Add_Api()
        {
            Type[] serviceTypes =
            {
                typeof(ICorsService),
                
                // typeof(IViewCompilerProvider),
                // typeof(RouteHandler)
            };

            Can_Add_Swagger();

            foreach (var serviceType in serviceTypes)
            {
                AssertService(serviceType);
            }
        }

        private void AssertService(Type type, bool addSwagger = true, bool shouldContain = true)
        {
            if (_apiModule == null)
            {
                _apiModule = new ApiModule(null,
                    new List<string>(), addSwagger);
                _apiModule.ConfigureServices(_serviceCollection);
            }

            _serviceCollection.Any(service => service.ServiceType == type).Should().Be(shouldContain);
        }
    }
}
