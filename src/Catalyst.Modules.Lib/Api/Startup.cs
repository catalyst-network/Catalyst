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
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Modules.Mempool.Models;
using Catalyst.Common.P2P.Models;
using Catalyst.Common.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpRepository.Repository;
using Swashbuckle.AspNetCore.Swagger;
using ILogger = Serilog.ILogger;

namespace Catalyst.Modules.Lib.Api
{
    public sealed class Startup
    {
        private readonly IContainerProvider _containerProvider;

        public Startup(IHostingEnvironment env, IContainerProvider containerProvider)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddEnvironmentVariables()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, NetworkTypes.Dev + ".json"));
            _containerProvider = containerProvider;
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
            });

            services.AddMvc()
               .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new Info { Title = "Catalyst API", Description = "Catalyst" });
            });
            var container = _containerProvider.Container;

            if (container != null)
            {
                services.AddSingleton(container.Resolve<IRepository<MempoolDocument, string>>());
                services.AddSingleton(container.Resolve<IRepository<Peer, string>>());
                services.AddSingleton(container.Resolve<IDeltaHashProvider>());
                services.AddSingleton(container.Resolve<IDfs>());
                services.AddSingleton(container.Resolve<ILogger>());
                services.AddSingleton(container.Resolve<IPeerRepository>());
                services.AddSingleton(container.Resolve<IMempoolRepository>());
            }
        }

        public IConfigurationRoot Configuration { get; private set; }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

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
