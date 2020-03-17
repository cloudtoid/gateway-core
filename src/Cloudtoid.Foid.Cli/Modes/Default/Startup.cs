namespace Cloudtoid.Foid.Cli.Modes.Default
{
    using Cloudtoid.Foid;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using static Contract;

    internal sealed class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = CheckValue(configuration, nameof(configuration));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            services.AddFoidProxy();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CheckValue(app, nameof(app));
            CheckValue(env, nameof(env));

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseFoidProxy();
        }

        internal static int Run()
        {
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .Build()
                .Run();

            return 0;
        }
    }
}
