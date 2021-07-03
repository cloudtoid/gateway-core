using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    internal sealed class NginxStartup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            CheckValue(services, nameof(services));
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CheckValue(app, nameof(app));
            CheckValue(env, nameof(env));
        }
    }
}
