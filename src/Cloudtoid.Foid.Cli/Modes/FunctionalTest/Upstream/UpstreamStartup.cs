namespace Cloudtoid.Foid.Cli.Modes.FunctionalTest.Upstream
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using static Contract;

    internal sealed class UpstreamStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            _ = services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CheckValue(app, nameof(app));
            CheckValue(env, nameof(env));

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        internal static IWebHost BuildWebHost()
        {
            return WebHost.CreateDefaultBuilder()
                .ConfigureKestrel(o => o.ListenLocalhost(Config.UpstreamPortNumber))
                .UseStartup<UpstreamStartup>()
                .Build();
        }
    }
}
