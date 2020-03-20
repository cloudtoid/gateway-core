namespace Cloudtoid.Foid.Cli.Modes.FunctionalTest.Proxy
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using static Contract;

    internal sealed class ProxyStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            _ = services.AddFoidProxy();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CheckValue(app, nameof(app));
            CheckValue(env, nameof(env));

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseFoidProxy();
        }

        internal static IWebHost BuildWebHost()
        {
            return WebHost.CreateDefaultBuilder()
                .ConfigureKestrel(o => o.ListenLocalhost(Config.ProxyPortNumber))
                .UseStartup<ProxyStartup>()
                .Build();
        }
    }
}
