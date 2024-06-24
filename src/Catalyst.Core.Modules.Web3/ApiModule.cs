#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Catalyst.Abstractions.Kvm;
using Catalyst.Core.Modules.Web3.Controllers.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Nethermind.Core;
using Nethermind.Serialization.Json;
using Serilog;
using Module = Autofac.Module;

namespace Catalyst.Core.Modules.Web3
{
    public sealed class ApiModule : Module
    {
        private readonly string _apiBindingAddress;
        private readonly List<string> _controllerModules;
        private readonly bool _addSwagger;

        public ApiModule(string apiBindingAddress, List<string> controllerModules, bool addSwagger = true)
        {
            _apiBindingAddress = apiBindingAddress;
            _controllerModules = controllerModules;
            _addSwagger = addSwagger;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var executingAssembly = Assembly.GetExecutingAssembly().Location;
            var buildPath = Path.GetDirectoryName(executingAssembly);
            var webDirectory = Directory.CreateDirectory(Path.Combine(buildPath, "wwwroot"));

            // Configure container
            builder.RegisterType<Web3HandlerResolver>().As<IWeb3HandlerResolver>().SingleInstance();
            builder.RegisterType<EthereumJsonSerializer>().As<IJsonSerializer>().SingleInstance();

            // Register controllers from specified modules
            foreach (var controllerModule in _controllerModules)
            {
                builder.RegisterAssemblyTypes(Assembly.Load(controllerModule))
                       .Where(t => t.Name.EndsWith("Controller"))
                       .AsSelf()
                       .InstancePerLifetimeScope();
            }

            // Build callback for integrating with ASP.NET Core host
            builder.RegisterBuildCallback(BuildCallback);
        }

        private void BuildCallback(ILifetimeScope scope)
        {
            var logger = scope.Resolve<ILogger>();

            try
            {
                Host.CreateDefaultBuilder()
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory()) // Use the scope directly here
                    .UseConsoleLifetime()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder
                            .UseUrls(_apiBindingAddress)
                            .UseWebRoot(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
                            .ConfigureServices(ConfigureServices)
                            .Configure(Configure);
                    })
                    .Build()
                    .Run();
            }
            catch (Exception e)
            {
                logger.Error(e, "Error loading API");
            }
        }



        private void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowOrigin",
                    builder => builder.AllowAnyOrigin()
                                      .AllowAnyMethod()
                                      .AllowAnyHeader());
            });

            services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new UInt256Converter());
                        options.JsonSerializerOptions.Converters.Add(new NullableUInt256Converter());
                        options.JsonSerializerOptions.Converters.Add(new AddressConverter());
                        options.JsonSerializerOptions.Converters.Add(new ByteArrayConverter());
                        options.JsonSerializerOptions.Converters.Add(new CidJsonConverter());
                    });

            if (_addSwagger)
            {
                services.AddSwaggerGen(swagger =>
                {
                    swagger.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalyst API", Description = "Catalyst" });
                });
            }
        }

        private void Configure(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowOrigin");
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "api/{controller}/{action}/{id?}");
            });

            if (_addSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(swagger =>
                {
                    swagger.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalyst API");
                });
            }
        }
    }
}
