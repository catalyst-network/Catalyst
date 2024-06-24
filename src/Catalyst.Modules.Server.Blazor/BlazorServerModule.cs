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
using System.Diagnostics;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Module = Autofac.Module;

namespace Catalyst.Modules.Server.Blazor
{
    public sealed class BlazorServerModule : Module
    {
        public static void Main(string[] args) { }

        private IHostBuilder _hostBuilder;
        private AutofacServiceProviderFactory _autofacServiceProviderFactory;

        protected override void Load(ContainerBuilder builder)
        {
            _autofacServiceProviderFactory = new AutofacServiceProviderFactory(builder);
            _hostBuilder = CreateHostBuilder();

            // Register the build callback with a lambda expression
            builder.RegisterBuildCallback(scope => Start(scope));
        }

        private void Start(ILifetimeScope container)
        {
            _autofacServiceProviderFactory.SetContainer(container);
            _ = container.Resolve<IHost>().RunAsync().ConfigureAwait(false);
        }

        public IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder(null)
               .ConfigureWebHostDefaults(webBuilder => { webBuilder.Configure(Configure); })
               .UseServiceProviderFactory(_autofacServiceProviderFactory)
               .ConfigureServices(ConfigureServices);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseExceptionHandler("/Error");

            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var listener = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton(listener);
            services.AddSingleton<DiagnosticSource>(listener);
            services.AddRazorPages();
            services.AddServerSideBlazor();
        }
    }
}
