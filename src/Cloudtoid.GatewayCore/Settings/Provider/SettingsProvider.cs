namespace Cloudtoid.GatewayCore.Settings
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.Extensions.Http;
    using Microsoft.Extensions.Options;
    using static Contract;

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
            var options = new HttpClientFactoryOptions();
            options.HttpMessageHandlerBuilderActions.Add(builder => ConfigureHttpMessageHandlerBuilder(builder, settings));

            httpFactoryOptionsMonitorCache.TryRemove(settings.HttpClientName);
            httpFactoryOptionsMonitorCache.TryAdd(settings.HttpClientName, options);
        }

        private void ConfigureHttpMessageHandlerBuilder(HttpMessageHandlerBuilder builder, UpstreamRequestSenderSettings settings)
        {
            CheckEqual(builder.Name, settings.HttpClientName, nameof(builder.Name));

            builder.PrimaryHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = settings.AllowAutoRedirect,
                UseCookies = settings.UseCookies,
            };
        }
    }
}
