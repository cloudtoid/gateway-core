using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Server
{
    internal sealed class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            services.AddGatewayCore();
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CheckValue(app, nameof(app));
            CheckValue(env, nameof(env));

            app.UseGatewayCore();
        }

        internal static Task StartAsync(IConfiguration config)
        {
            var gatewayConfig = config.GetSection("gateway");
            var kestrelConfig = config.GetSection("server");

            // This is a small portion of Kestrel that does what we need and no more.
            return new WebHostBuilder()
                .ConfigureServices(s => s.Configure<GatewayOptions>(gatewayConfig))
                .UseKestrel((c, o) =>
                {
                    o.AddServerHeader = false;
                    o.Configure(kestrelConfig, reloadOnChange: true);
                })
                .UseStartup<Startup>()
                .Build()
                .StartAsync();
        }
    }
}
