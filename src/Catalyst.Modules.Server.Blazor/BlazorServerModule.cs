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
    public class BlazorServerModule : Module
    {
        public static void Main(string[] args) { }

        private IHostBuilder _hostBuilder;
        private AutofacServiceProviderFactory _autofacServiceProviderFactory;

        protected override void Load(ContainerBuilder builder)
        {
            _autofacServiceProviderFactory = new AutofacServiceProviderFactory(builder);
            _hostBuilder = CreateHostBuilder();
            try
            {
                _hostBuilder.Build();
            }
            catch (Exception)
            {
                //Ignored exception as the server cannot start without container being built
            }

            builder.RegisterBuildCallback(Start);
        }

        private void Start(IContainer container)
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
