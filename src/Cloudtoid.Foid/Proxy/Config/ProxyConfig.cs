namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Extensions.Configuration;
    using static Contract;
    using UpstreamRequestHeaders = ConfigConstants.Proxy.UpstreamRequest.Headers;

    public sealed class ProxyConfig
    {
        private readonly IConfiguration config;

        public ProxyConfig(IConfiguration config)
        {
            this.config = CheckValue(config, nameof(config));
            Values = new ConfigValues(config);
            RegisterChangeCallback();
        }

        public ConfigValues Values { get; private set; }

        internal AutoResetEvent ChangeEvent { get; } = new AutoResetEvent(false);

        private void RegisterChangeCallback()
            => config.GetReloadToken().RegisterChangeCallback(_ => OnConfigChanged(), default);

        private void OnConfigChanged()
        {
            Values = new ConfigValues(config);
            RegisterChangeCallback();
            ChangeEvent.Set();
        }

        public sealed class ConfigValues
        {
            internal ConfigValues(IConfiguration config)
            {
                TotalTimeout = TimeSpan.FromMilliseconds(config.GetValueSafe(ConfigConstants.Proxy.TotalTimeoutInMilliseconds, 240000));
                UpstreamRequest = new UpstreamRequestValues(config);
            }

            /// <summary>
            /// This is the total timeout in milliseconds that the <see cref="ProxyMiddleware"/>
            /// has to proxy the call to the upstream service.
            /// </summary>
            internal TimeSpan TotalTimeout { get; }

            internal UpstreamRequestValues UpstreamRequest { get; }

            public sealed class UpstreamRequestValues
            {
                internal UpstreamRequestValues(IConfiguration config)
                {
                    Headers = new HeadersValues(config);
                }

                internal HeadersValues Headers { get; }

                public sealed class HeadersValues
                {
                    internal HeadersValues(IConfiguration config)
                    {
                        AllowHeadersWithEmptyValue = config.GetValueSafe(UpstreamRequestHeaders.AllowHeadersWithEmptyValue, false);
                        AllowHeadersWithUnderscoreInName = config.GetValueSafe(UpstreamRequestHeaders.AllowHeadersWithUnderscoreInName, false);
                        IncludeExternalAddress = config.GetValueSafe(UpstreamRequestHeaders.IncludeExternalAddress, false);
                        IgnoreClientAddress = config.GetValueSafe(UpstreamRequestHeaders.IgnoreClientAddress, false);
                        IgnoreClientProtocol = config.GetValueSafe(UpstreamRequestHeaders.IgnoreClientProtocol, false);
                        IgnoreRequestId = config.GetValueSafe(UpstreamRequestHeaders.IgnoreRequestId, false);
                        IgnoreCallId = config.GetValueSafe(UpstreamRequestHeaders.IgnoreCallId, false);
                        DefaultHost = config.GetValueSafe(UpstreamRequestHeaders.DefaultHost, Environment.MachineName);
                        ProxyName = config.GetValueSafe(UpstreamRequestHeaders.ProxyName, "cloudtoid-foid");
                        ExtraHeaders = config.GetValueSafe(UpstreamRequestHeaders.ExtraHeaders, List.Empty<(string Key, IEnumerable<string> Values)>());
                    }

                    /// <summary>
                    /// By default, headers with an empty value are dropped.
                    /// </summary>
                    public bool AllowHeadersWithEmptyValue { get; }

                    /// <summary>
                    /// By default, headers with an underscore in their names are dropped.
                    /// </summary>
                    public bool AllowHeadersWithUnderscoreInName { get; }

                    /// <summary>
                    /// If true, an "x-foid-external-address" header with the immediate downstream IP address is added to the outgoing upstream call.
                    /// The default value is false.
                    /// </summary>
                    public bool IncludeExternalAddress { get; }

                    /// <summary>
                    /// If false, it will append the IP address of the nearest client to the "x-forwarded-for" header.
                    /// The default value is false.
                    /// </summary>
                    public bool IgnoreClientAddress { get; }

                    /// <summary>
                    /// If false, it will append the client protocol (HTTP or HTTPS) to the "x-forwarded-proto" header.
                    /// The default value is false.
                    /// </summary>
                    public bool IgnoreClientProtocol { get; }

                    /// <summary>
                    /// If false, it will append a "x-request-id" header if not present.
                    /// The default value is false.
                    /// </summary>
                    public bool IgnoreRequestId { get; }

                    /// <summary>
                    /// If false, it will append a "x-call-id" header. This is a guid that is always new for each call.
                    /// The default value is false.
                    /// </summary>
                    public bool IgnoreCallId { get; }

                    /// <summary>
                    /// If the incoming downstream request does not have a HOST header, the value provided here will be used.
                    /// </summary>
                    public string DefaultHost { get; }

                    /// <summary>
                    /// If this is not null, an "x-foid-proxy-name" header with this value is added to the outgoing upstream call.
                    /// </summary>
                    public string? ProxyName { get; }

                    /// <summary>
                    /// Extra headers to be appended to the outgoing upstream request
                    /// </summary>
                    public IReadOnlyList<(string Key, IEnumerable<string> Values)> ExtraHeaders { get; }
                }
            }
        }
    }
}
