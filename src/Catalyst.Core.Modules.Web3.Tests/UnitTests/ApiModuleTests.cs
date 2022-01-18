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
using NUnit.Framework;
using System.Net;
using Catalyst.Core.Modules.Web3.Options;

namespace Catalyst.Core.Modules.Web3.Client.Tests.UnitTests
{
    public sealed class ApiModuleTests
    {
        [Test]
        public void Can_Add_Swagger() {
            ServiceCollection serviceCollection = new();
            ApiModule apiModule = new(new HttpOptions(new IPEndPoint(IPAddress.Any, 5005)), new List<string>(), true);
            apiModule.ConfigureServices(serviceCollection);
            serviceCollection.Any(service => service.ServiceType == typeof(ISwaggerProvider)).Should().Be(true);
        }

        [Test]
        public void Can_Not_Add_Swagger() {
            ServiceCollection serviceCollection = new();
            ApiModule apiModule = new(new HttpOptions(new IPEndPoint(IPAddress.Any, 5005)), new List<string>(), false);
            apiModule.ConfigureServices(serviceCollection);
            serviceCollection.Any(service => service.ServiceType == typeof(ISwaggerProvider)).Should().Be(false);
        }

        [Test]
        public void Can_Add_Api()
        {
            Type[] serviceTypes =
            {
                typeof(ICorsService),
            };

            ServiceCollection serviceCollection = new();
            ApiModule apiModule = new(new HttpOptions(new IPEndPoint(IPAddress.Any, 5005)), new List<string>(), true);
            apiModule.ConfigureServices(serviceCollection);

            foreach (var serviceType in serviceTypes)
            {
                serviceCollection.Any(service => service.ServiceType == serviceType).Should().Be(true);
            }
        }
    }
}
