namespace Cloudtoid.GatewayCore.Server
{
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using static Contract;

    internal sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            services.AddGatewayCore();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CheckValue(app, nameof(app));
            CheckValue(env, nameof(env));

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseGatewayCore();
        }

        internal static Task StartAsync(IConfiguration config)
        {
            var gatewayConfig = config.GetSection("gateway");
            var kestrelConfig = config.GetSection("server");

            return WebHost.CreateDefaultBuilder()
                .ConfigureServices(s => s.Configure<GatewayOptions>(gatewayConfig))
                .ConfigureServices(s => s.Configure<KestrelServerOptions>(kestrelConfig))
                .UseStartup<Startup>()
                .Build()
                .StartAsync();
        }
    }
}
