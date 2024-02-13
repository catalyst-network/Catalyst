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
        private readonly string[] _controllerModules;
        private IContainer _container;
        private readonly bool _addSwagger;

        public ApiModule(string apiBindingAddress, List<string> controllerModules, bool addSwagger = true)
        {
            _apiBindingAddress = apiBindingAddress;
            _controllerModules = controllerModules.ToArray();
            _addSwagger = addSwagger;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var executingAssembly = Assembly.GetExecutingAssembly().Location;
            var buildPath = Path.GetDirectoryName(executingAssembly);
            var webDirectory = Directory.CreateDirectory(Path.Combine(buildPath, "wwwroot"));

            // Adjusted to use IComponentContext
            async void BuildCallback(IComponentContext context)
            {
                var logger = context.Resolve<ILogger>();

                try
                {
                    await Host.CreateDefaultBuilder()
                        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                        .ConfigureWebHostDefaults(webHostBuilder =>
                        {
                            webHostBuilder
                                .ConfigureServices(services =>
                                {
                                    // If you need to add services to the IServiceCollection, do it here
                                    ConfigureServices(services);
                                })
                                .Configure(app =>
                                {
                                    // Application configuration
                                    Configure(app.ApplicationServices.GetRequiredService<IApplicationBuilder>());
                                })
                                .UseUrls(_apiBindingAddress)
                                .UseWebRoot(webDirectory.FullName);
                            // .UseSerilog(); // Uncomment if Serilog is being used
                        }).Build().RunAsync();

                    // Consider application lifecycle management here if needed
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error loading API");
                }
            }

            // Register the callback with the correct signature
            builder.RegisterBuildCallback(BuildCallback);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<Web3HandlerResolver>().As<IWeb3HandlerResolver>().SingleInstance();
            builder.RegisterType<EthereumJsonSerializer>().As<IJsonSerializer>().SingleInstance();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options =>
                    options.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader());
            });

            services.AddMvc().AddJsonOptions(options =>
            {
                var converters = options.JsonSerializerOptions.Converters;

                converters.Add(new UInt256Converter());
                converters.Add(new NullableUInt256Converter());
                converters.Add(new AddressConverter());
                converters.Add(new ByteArrayConverter());
                converters.Add(new CidJsonConverter());
            });//.AddApiExplorer();

            var mvcBuilder = services.AddRazorPages();

            _controllerModules.ToList().ForEach(controller => mvcBuilder.AddApplicationPart(Assembly.Load(controller)));

            mvcBuilder.AddControllersAsServices();

            if (_addSwagger)
                services.AddSwaggerGen(swagger =>
                {
                    swagger.SwaggerDoc("v1", new OpenApiInfo {Title = "Catalyst API", Description = "Catalyst"});
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseDeveloperExceptionPage();
            app.UseCors("AllowOrigin");
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("CatalystApi", "api/{controller}/{action}/{id}");
            });

            if (_addSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(swagger => { swagger.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalyst API"); });
            }
        }
        
        private sealed class SharedContainerProviderFactory : IServiceProviderFactory<ContainerBuilder>
        {
            readonly IContainer _container;

            public SharedContainerProviderFactory(IContainer container)
            {
                _container = container;
            }

            public ContainerBuilder CreateBuilder(IServiceCollection services)
            {
                var builder = new ContainerBuilder();
                builder.Populate(services);
                return builder;
            }

            public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
            {
                // using an obsolete way of updating an already created container
                // containerBuilder.Update(_container);

                return new AutofacServiceProvider(_container);
            }
        }
    }
}
