namespace Cloudtoid.GatewayCore.Cli.Modes.FunctionalTest.Proxy
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
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

        internal static IWebHost BuildWebHost(int port, string configFile)
        {
            var config = LoadConfig(configFile);
            return WebHost.CreateDefaultBuilder()
                .ConfigureServices(s => s.Configure<GatewayOptions>(config))
                .ConfigureKestrel(o => o.ListenLocalhost(port))
                .UseStartup<ProxyStartup>()
                .Build();
        }

        private static IConfiguration LoadConfig(string configFile)
        {
            var file = new FileInfo(configFile);
            if (!file.Exists)
                throw new InvalidOperationException($"File '{configFile}' doesn't exist!");

            var configBuilder = new ConfigurationBuilder()
               .SetBasePath(file.Directory.FullName)
               .AddJsonFile(file.Name, optional: false, reloadOnChange: false);

            return configBuilder.Build();
        }
    }
}
