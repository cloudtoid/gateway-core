namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using static Contract;

    internal sealed class ProxyStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            _ = services.AddGatewayCore();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CheckValue(app, nameof(app));
            CheckValue(env, nameof(env));

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseGatewayCore();
        }

        internal static IWebHost BuildWebHost(int port, IConfiguration config)
        {
            return WebHost.CreateDefaultBuilder()
                .ConfigureServices(s => s.Configure<GatewayOptions>(config))
                .ConfigureKestrel(o => o.ListenLocalhost(port, lo => lo.Protocols = HttpProtocols.Http1AndHttp2))
                .UseStartup<ProxyStartup>()
                .Build();
        }
    }
}
