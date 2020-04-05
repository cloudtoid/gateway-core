namespace Cloudtoid.GatewayCore.Cli.Modes.Default
{
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
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

        internal static async Task StartAsync()
        {
            await WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .Build()
                .StartAsync();
        }
    }
}
