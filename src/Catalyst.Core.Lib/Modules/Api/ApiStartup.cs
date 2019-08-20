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
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Catalyst.Common.Config;
using Catalyst.Common.Kernel;
using Catalyst.Common.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Catalyst.Core.Lib.Modules.Api
{
    public sealed class ApiStartup
    {
        private readonly ContainerBuilder _containerBuilder;

        public ApiStartup(IHostingEnvironment env, ContainerBuilder containerBuilder)
        {
            _containerBuilder = containerBuilder;
            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddEnvironmentVariables()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, NetworkTypes.Dev + ".json"));
            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            _containerBuilder.Populate(services);
            return null;
        }

        public IConfigurationRoot Configuration { get; }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            if (Kernel.Instance == null)
            {
                return;
            }

            app.ApplicationServices = Kernel.Instance.NewServiceCollection();
            app.UseDeveloperExceptionPage();
            app.UseCors(options => options.AllowAnyOrigin());
            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "CatalystApi", template: "api/{controller}/{action}/{id}");
            });

            app.UseSwagger();
            app.UseSwaggerUI(swagger =>
            {
                swagger.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalyst API");
            });
        }
    }
}
