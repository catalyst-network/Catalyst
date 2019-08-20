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

using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using Module = Autofac.Module;

namespace Catalyst.Core.Lib.Modules.Api
{
    public class ApiModule : Module
    {
        private readonly string _apiBindingAddress;
        private readonly string[] _controllerModules;
        private IContainer _container;

        public ApiModule(string apiBindingAddress, List<string> controllerModules)
        {
            _apiBindingAddress = apiBindingAddress;
            _controllerModules = controllerModules.ToArray();
        }

        protected override void Load(ContainerBuilder builder)
        {
            var host = WebHost.CreateDefaultBuilder()
               .ConfigureServices(serviceCollection => ConfigureServices(serviceCollection, builder))
               .Configure(Configure)
               .UseUrls(_apiBindingAddress)
               .UseSerilog()
               .Build();

            builder.RegisterInstance(host);
            builder.RegisterBuildCallback(async container =>
            {
                _container = container;
                await host.StartAsync();
            });
            base.Load(builder);
        }

        public void ConfigureServices(IServiceCollection services, ContainerBuilder containerBuilder)
        {
            services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
            });

            var mvcBuilder = services.AddMvc();

            foreach (var controllerModule in _controllerModules)
            {
                mvcBuilder.AddApplicationPart(Assembly.Load(controllerModule));
            }

            mvcBuilder.AddControllersAsServices();

            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new Info {Title = "Catalyst API", Description = "Catalyst"});
            });

            containerBuilder.Populate(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.ApplicationServices = new AutofacServiceProvider(_container);
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
