using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Settings
{
    internal sealed class SettingsProvider : ISettingsProvider
    {
        private readonly ISettingsCreator settingsCreator;
        private readonly IOptionsMonitor<GatewayOptions> options;
        private readonly IOptionsMonitorCache<HttpClientFactoryOptions> httpFactoryOptionsMonitorCache;

        public SettingsProvider(
            ISettingsCreator settingsCreator,
            IOptionsMonitor<GatewayOptions> options,
            IOptionsMonitorCache<HttpClientFactoryOptions> httpFactoryOptionsMonitorCache)
        {
            this.settingsCreator = CheckValue(settingsCreator, nameof(settingsCreator));
            this.options = CheckValue(options, nameof(options));
            this.httpFactoryOptionsMonitorCache = CheckValue(httpFactoryOptionsMonitorCache, nameof(httpFactoryOptionsMonitorCache));

            options.OnChange(_ => CurrentValue = CreateSettings());
            CurrentValue = CreateSettings();
        }

        public GatewaySettings CurrentValue { get; private set; }

        private GatewaySettings CreateSettings()
        {
            var settings = settingsCreator.Create(options.CurrentValue);
            CreateHttpClientFactoryOptions(settings.Routes);
            return settings;
        }

        private void CreateHttpClientFactoryOptions(IReadOnlyList<RouteSettings> routes)
        {
            foreach (var route in routes)
            {
                if (route.Proxy is null)
                    continue;

                CreateHttpClientFactoryOptions(route.Proxy.UpstreamRequest.Sender);
            }
        }

        private void CreateHttpClientFactoryOptions(UpstreamRequestSenderSettings settings)
        {
            httpFactoryOptionsMonitorCache.TryRemove(settings.HttpClientName);

            var options = new HttpClientFactoryOptions();
            options.HttpMessageHandlerBuilderActions.Add(builder => ConfigureHttpMessageHandlerBuilder(builder, settings));

            httpFactoryOptionsMonitorCache.TryAdd(settings.HttpClientName, options);
        }

        private void ConfigureHttpMessageHandlerBuilder(HttpMessageHandlerBuilder builder, UpstreamRequestSenderSettings settings)
        {
            CheckEqual(builder.Name, settings.HttpClientName, nameof(builder.Name));

            builder.PrimaryHandler = new SocketsHttpHandler
            {
                ConnectTimeout = settings.ConnectTimeout,
                Expect100ContinueTimeout = settings.Expect100ContinueTimeout,
                PooledConnectionIdleTimeout = settings.PooledConnectionIdleTimeout,
                PooledConnectionLifetime = settings.PooledConnectionLifetime,
                ResponseDrainTimeout = settings.ResponseDrainTimeout,
                MaxAutomaticRedirections = settings.MaxAutomaticRedirections,
                MaxConnectionsPerServer = settings.MaxConnectionsPerServer,
                MaxResponseDrainSize = settings.MaxResponseDrainSizeInBytes,
                MaxResponseHeadersLength = settings.MaxResponseHeadersLengthInKilobytes,
                AllowAutoRedirect = settings.AllowAutoRedirect,
                UseCookies = settings.UseCookies,
            };
        }
    }
}
