using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    internal sealed class UpstreamStartup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            _ = services.AddControllers();
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CheckValue(app, nameof(app));
            CheckValue(env, nameof(env));

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        internal static IWebHost BuildWebHost(int port)
        {
            return WebHost.CreateDefaultBuilder()
                .ConfigureKestrel(o =>
                {
                    o.ListenLocalhost(
                        port,
                        lo =>
                        {
                            lo.Protocols = HttpProtocols.Http2;

                            // Kestrel on MacOS does not support TLS on HTTP/2.0
                            // Follow the issue here: https://github.com/dotnet/runtime/issues/27727
                            // TODO: Remove this once the issue is fixed.
                            if (!OperatingSystem.IsMacOS())
                                lo.UseHttps();
                        });
                })
                .UseStartup<UpstreamStartup>()
                .Build();
        }
    }
}
