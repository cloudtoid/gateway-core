using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cloudtoid.GatewayCore.Expression;
using Cloudtoid.UrlPattern;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using static Cloudtoid.Contract;
using static Cloudtoid.GatewayCore.GatewayOptions;
using static Cloudtoid.GatewayCore.GatewayOptions.RouteOptions;
using static Cloudtoid.GatewayCore.GatewayOptions.RouteOptions.ProxyOptions;
using static Cloudtoid.GatewayCore.GatewayOptions.RouteOptions.ProxyOptions.DownstreamResponseOptions.HeadersOptions;
using SenderDefaults = Cloudtoid.GatewayCore.Settings.Defaults.Route.Proxy.Upstream.Request.Sender;

namespace Cloudtoid.GatewayCore.Settings
{
    internal sealed class SettingsCreator : ISettingsCreator
    {
        private static readonly IDictionary<string, SameSiteMode> SameSiteModes = new Dictionary<string, SameSiteMode>(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = SameSiteMode.None,
            ["lax"] = SameSiteMode.Lax,
            ["strict"] = SameSiteMode.Strict,
        };

        private readonly IPatternEngine patternEngine;
        private readonly IGuidProvider guidProvider;
        private readonly ILogger<SettingsCreator> logger;

        public SettingsCreator(
            IPatternEngine patternEngine,
            IGuidProvider guidProvider,
            ILogger<SettingsCreator> logger)
        {
            this.patternEngine = CheckValue(patternEngine, nameof(patternEngine));
            this.guidProvider = CheckValue(guidProvider, nameof(guidProvider));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public GatewaySettings Create(GatewayOptions options)
        {
            var system = Create(options.System);

            var routes = options.Routes
                .Select(o => Create(o.Key, o.Value))
                .WhereNotNull()
                .ToArray();

            if (routes.Length == 0)
                LogError("No routes are specified.");

            return new GatewaySettings(system, routes);
        }

        private SystemSettings Create(SystemOptions options)
        {
            if (!options.RouteCacheMaxCount.HasValue)
                return new SystemSettings(Defaults.System.RouteCacheMaxCount);

            if (options.RouteCacheMaxCount.Value < 1000)
            {
                LogError($"{nameof(options.RouteCacheMaxCount)} must be set to at least 1000. Using the default value which is {Defaults.System.RouteCacheMaxCount} instead.");
                return new SystemSettings(Defaults.System.RouteCacheMaxCount);
            }

            return new SystemSettings(options.RouteCacheMaxCount.Value);
        }

        private RouteSettings? Create(
            string route,
            RouteOptions options)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                LogError("A route cannot be an empty string.");
                return null;
            }

            // 1) Compile the route pattern
            var compiledRoute = Compile(route);
            if (compiledRoute is null)
                return null;

            // 2) Build a trie of the variables found in the route pattern. This trie will be used when evaluating expressions.
            var variableTrie = new VariableTrie<string>(
                compiledRoute.VariableNames.Select(v => (v, v)));

            return new RouteSettings(
                route,
                compiledRoute,
                variableTrie,
                Create(route, options.Proxy));
        }

        private CompiledPattern? Compile(string route)
        {
            if (!patternEngine.TryCompile(route, out var compiledRoute, out var compilerErrors))
            {
                LogErrors(route, compilerErrors);
                return null;
            }

            foreach (var variableName in compiledRoute.VariableNames)
            {
                if (SystemVariableNames.Names.Contains(variableName))
                {
                    LogError(route, $"The variable name '{variableName}' collides with a system variable with the same name.");
                    return null;
                }
            }

            return compiledRoute;
        }

        private ProxySettings? Create(
            string route,
            ProxyOptions? options)
        {
            if (options is null)
                return null;

            if (string.IsNullOrWhiteSpace(options.To))
            {
                LogError(route, $"The '{nameof(options.To)}' cannot be empty or skipped.");
                return null;
            }

            var correlationIdHeader = string.IsNullOrWhiteSpace(options.CorrelationIdHeader)
                ? null
                : options.CorrelationIdHeader;

            return new ProxySettings(
                options.To,
                options.ProxyName,
                correlationIdHeader,
                Create(route, options.UpstreamRequest, options.ProxyName is not null),
                Create(route, options.DownstreamResponse));
        }

        private UpstreamRequestSettings Create(
            string route,
            UpstreamRequestOptions options,
            bool addProxyName)
        {
            return new UpstreamRequestSettings(
                options.HttpVersion,
                Create(route, options.Headers, addProxyName),
                Create(options.Sender));
        }

        private UpstreamRequestHeadersSettings Create(
            string route,
            UpstreamRequestOptions.HeadersOptions options,
            bool addProxyName)
        {
            return new UpstreamRequestHeadersSettings(
                options.DiscardInboundHeaders,
                options.DiscardEmpty,
                options.DiscardUnderscore,
                options.Discards,
                options.AddExternalAddress,
                addProxyName,
                options.SkipCorrelationId,
                options.SkipCallId,
                options.SkipVia,
                options.SkipForwarded,
                options.UseXForwarded,
                Create(route, options.Overrides));
        }

        private UpstreamRequestSenderSettings Create(
            UpstreamRequestOptions.SenderOptions options)
        {
            // every time a route is created, we need to create a new named HTTP Client for it.
            // This name is used by the IHttpClientFactory to get instances of HttpClient configured
            // with the settings specified by UpstreamRequestSenderSettings.
            var httpClientName = options.HttpClientName ?? guidProvider.NewGuid().ToStringInvariant("N");

            var connectTimeout = options.ConnectTimeoutInMilliseconds.HasValue
                ? TimeSpan.FromMilliseconds(options.ConnectTimeoutInMilliseconds.Value)
                : SenderDefaults.ConnectTimeout;

            var expect100ContinueTimeout = options.Expect100ContinueTimeoutInMilliseconds.HasValue
                ? TimeSpan.FromMilliseconds(options.Expect100ContinueTimeoutInMilliseconds.Value)
                : SenderDefaults.Expect100ContinueTimeout;

            var pooledConnectionIdleTimeout = options.PooledConnectionIdleTimeoutInMilliseconds.HasValue
                ? TimeSpan.FromMilliseconds(options.PooledConnectionIdleTimeoutInMilliseconds.Value)
                : SenderDefaults.PooledConnectionIdleTimeout;

            var pooledConnectionLifetime = options.PooledConnectionLifetimeInMilliseconds.HasValue
                ? TimeSpan.FromMilliseconds(options.PooledConnectionLifetimeInMilliseconds.Value)
                : SenderDefaults.PooledConnectionLifetime;

            var responseDrainTimeout = options.ResponseDrainTimeoutInMilliseconds.HasValue
                ? TimeSpan.FromMilliseconds(options.ResponseDrainTimeoutInMilliseconds.Value)
                : SenderDefaults.ResponseDrainTimeout;

            return new UpstreamRequestSenderSettings(
                httpClientName,
                options.TimeoutInMilliseconds,
                connectTimeout,
                expect100ContinueTimeout,
                pooledConnectionIdleTimeout,
                pooledConnectionLifetime,
                responseDrainTimeout,
                options.MaxAutomaticRedirections ?? SenderDefaults.MaxAutomaticRedirections,
                options.MaxConnectionsPerServer ?? SenderDefaults.MaxConnectionsPerServer,
                options.MaxResponseDrainSizeInBytes ?? SenderDefaults.MaxResponseDrainSizeInBytes,
                options.MaxResponseHeadersLengthInKilobytes ?? SenderDefaults.MaxResponseHeadersLengthInKilobytes,
                options.AllowAutoRedirect,
                options.UseCookies);
        }

        private IReadOnlyDictionary<string, CookieSettings> Create(
            string route,
            Dictionary<string, CookieOptions> options)
        {
            if (options.Count == 0)
                return ImmutableDictionary<string, CookieSettings>.Empty;

            return options
                .Select(c => Create(route, c.Key, c.Value))
                .WhereNotNull()
                .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        }

        private CookieSettings? Create(
            string route,
            string name,
            CookieOptions option)
        {
            var sameSite = SameSiteMode.Unspecified;
            if (option.SameSite is not null)
            {
                if (!SameSiteModes.TryGetValue(option.SameSite, out var site))
                {
                    LogError(route, $"The '{option.SameSite}' is not a valid value for '{name}' cookie's SameSite attribute. Valid values are 'none', 'lax' and 'strict'.");
                    return null;
                }

                sameSite = site;
            }

            return new CookieSettings(name, option.Secure, option.HttpOnly, sameSite, option.Domain);
        }

        private IReadOnlyDictionary<string, HeaderOverride> Create(
            string route,
            Dictionary<string, string[]> headers)
        {
            if (headers.Count == 0)
                return ImmutableDictionary<string, HeaderOverride>.Empty;

            return headers
                .Select(h => Create(route, h.Key, h.Value))
                .WhereNotNull()
                .ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);
        }

        private DownstreamResponseSettings Create(
            string route,
            DownstreamResponseOptions options)
        {
            return new DownstreamResponseSettings(
                Create(route, options.Headers));
        }

        private DownstreamResponseHeadersSettings Create(
            string route,
            DownstreamResponseOptions.HeadersOptions options)
        {
            return new DownstreamResponseHeadersSettings(
                options.DiscardInboundHeaders,
                options.DiscardEmpty,
                options.DiscardUnderscore,
                options.Discards,
                options.AddServer,
                options.AddCorrelationId,
                options.AddCallId,
                options.SkipVia,
                Create(route, options.Cookies),
                Create(route, options.Overrides));
        }

        private HeaderOverride? Create(
            string route,
            string headerName,
            string[] headerValuesExpressions)
        {
            if (!HttpHeader.IsValidName(headerName))
            {
                LogWarning(route, $"The '{headerName}' is not a valid HTTP header name. It will be ignored.");
                return null;
            }

            if (headerValuesExpressions.IsNullOrEmpty())
            {
                LogWarning(route, $"The '{headerName}' is either null or empty. It will be ignored.");
                return null;
            }

            return new HeaderOverride(
                headerName,
                headerValuesExpressions);
        }

        private void LogError(string message)
            => logger.LogError($"Configuration error: {message}");

        private void LogError(string route, string message)
            => logger.LogError($"Configuration error: ({route}) {message}");

        private void LogWarning(string route, string message)
            => logger.LogWarning($"Configuration warning: ({route}) {message}");

        private void LogErrors(string route, IReadOnlyList<PatternCompilerError> errors)
        {
            foreach (var error in errors)
                LogError(route, error.ToString());
        }
    }
}
