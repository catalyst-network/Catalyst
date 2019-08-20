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
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;

namespace Catalyst.Core.Lib.Modules.Api
{
    public class ApiModule : JsonConfiguredModule
    {
        private readonly string _apiBindingAddress;
        private readonly string[] _controllerModules;

        public ApiModule(string configFilePath, string apiBindingAddress, List<string> controllerModules) : base(configFilePath)
        {
            _apiBindingAddress = apiBindingAddress;
            _controllerModules = controllerModules.ToArray();
        }

        protected override void Load(ContainerBuilder builder)
        {
            var webHost = WebHost.CreateDefaultBuilder()
               .ConfigureServices(services =>
                {
                    services.AddSingleton(builder);
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
                })
               .UseUrls(_apiBindingAddress)
               .UseStartup<ApiStartup>()
               .UseSerilog()
               .Build();
            builder.RegisterInstance(webHost).As<IWebHost>();

            base.Load(builder);
        }
    }
}
