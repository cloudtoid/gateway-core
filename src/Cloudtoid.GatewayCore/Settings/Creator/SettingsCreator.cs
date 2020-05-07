namespace Cloudtoid.GatewayCore.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cloudtoid.GatewayCore.Expression;
    using Cloudtoid.UrlPattern;
    using Microsoft.Extensions.Logging;
    using static Cloudtoid.GatewayCore.GatewayOptions;
    using static Cloudtoid.GatewayCore.GatewayOptions.RouteOptions;
    using static Cloudtoid.GatewayCore.GatewayOptions.RouteOptions.ProxyOptions;
    using static Contract;
    using SenderDefaults = Defaults.Route.Proxy.Upstream.Request.Sender;

    internal sealed class SettingsCreator : ISettingsCreator
    {
        private readonly IExpressionEvaluator evaluator;
        private readonly IPatternEngine patternEngine;
        private readonly IGuidProvider guidProvider;
        private readonly ILogger<SettingsCreator> logger;

        public SettingsCreator(
            IExpressionEvaluator evaluator,
            IPatternEngine patternEngine,
            IGuidProvider guidProvider,
            ILogger<SettingsCreator> logger)
        {
            this.evaluator = CheckValue(evaluator, nameof(evaluator));
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
            var context = new RouteSettingsContext(route, evaluator);
            var compiledRoute = Compile(context);
            if (compiledRoute is null)
                return null;

            // 2) Build a trie of the variables found in the route pattern. This trie will be used when evaluating expressions.
            var variableTrie = new VariableTrie<string>(
                compiledRoute.VariableNames.Select(v => (v, v)));

            return new RouteSettings(
                route,
                compiledRoute,
                variableTrie,
                Create(context, options.Proxy));
        }

        private CompiledPattern? Compile(RouteSettingsContext context)
        {
            if (!patternEngine.TryCompile(context.Route, out var compiledRoute, out var compilerErrors))
            {
                LogErrors(context, compilerErrors);
                return null;
            }

            foreach (var variableName in compiledRoute.VariableNames)
            {
                if (SystemVariableNames.Names.Contains(variableName))
                {
                    LogError(context, $"The variable name '{variableName}' collides with a system variable with the same name.");
                    return null;
                }
            }

            return compiledRoute;
        }

        private ProxySettings? Create(
            RouteSettingsContext context,
            ProxyOptions? options)
        {
            if (options is null)
                return null;

            if (string.IsNullOrWhiteSpace(options.To))
            {
                LogError(context, $"The '{nameof(options.To)}' cannot be empty or skipped.");
                return null;
            }

            var correlationIdHeader = string.IsNullOrWhiteSpace(options.CorrelationIdHeader)
                ? null
                : options.CorrelationIdHeader;

            return new ProxySettings(
                context,
                options.To,
                options.ProxyName,
                correlationIdHeader,
                Create(context, options.UpstreamRequest, options.ProxyName != null),
                Create(context, options.DownstreamResponse));
        }

        private UpstreamRequestSettings Create(
            RouteSettingsContext context,
            UpstreamRequestOptions options,
            bool includeProxyName)
        {
            return new UpstreamRequestSettings(
                context,
                options.HttpVersion,
                Create(context, options.Headers, includeProxyName),
                Create(context, options.Sender));
        }

        private UpstreamRequestHeadersSettings Create(
            RouteSettingsContext context,
            UpstreamRequestOptions.HeadersOptions options,
            bool includeProxyName)
        {
            return new UpstreamRequestHeadersSettings(
                options.AllowHeadersWithEmptyValue,
                options.AllowHeadersWithUnderscoreInName,
                options.IncludeExternalAddress,
                includeProxyName,
                options.IgnoreAllDownstreamHeaders,
                options.IgnoreVia,
                options.IgnoreCorrelationId,
                options.IgnoreCallId,
                options.IgnoreForwarded,
                options.UseXForwarded,
                Create(context, options.Overrides));
        }

        private UpstreamRequestSenderSettings Create(
            RouteSettingsContext context,
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
                context,
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

        private IReadOnlyList<HeaderOverride> Create(
            RouteSettingsContext context,
            Dictionary<string, string[]> headers)
        {
            return headers
                .Select(h => Create(context, h.Key, h.Value))
                .WhereNotNull()
                .ToArray();
        }

        private DownstreamResponseSettings Create(
            RouteSettingsContext context,
            DownstreamResponseOptions options)
        {
            return new DownstreamResponseSettings(
                Create(context, options.Headers));
        }

        private DownstreamResponseHeadersSettings Create(
            RouteSettingsContext context,
            DownstreamResponseOptions.HeadersOptions options)
        {
            return new DownstreamResponseHeadersSettings(
                options.AllowHeadersWithEmptyValue,
                options.AllowHeadersWithUnderscoreInName,
                options.IgnoreAllUpstreamHeaders,
                options.IgnoreVia,
                options.IncludeCorrelationId,
                options.IncludeCallId,
                Create(context, options.Overrides));
        }

        private HeaderOverride? Create(
            RouteSettingsContext context,
            string headerName,
            string[] headerValuesExpressions)
        {
            if (!HttpHeader.IsValidName(headerName))
            {
                LogWarning(context, $"The '{headerName}' is not a valid HTTP header name. It will be ignored.");
                return null;
            }

            return new HeaderOverride(
                context,
                headerName,
                headerValuesExpressions ?? Array.Empty<string>());
        }

        private void LogError(string message)
            => logger.LogError($"Configuration error: {message}");

        private void LogError(RouteSettingsContext context, string message)
            => logger.LogError($"Configuration error: ({context.Route}) {message}");

        private void LogWarning(RouteSettingsContext context, string message)
            => logger.LogWarning($"Configuration warning: ({context.Route}) {message}");

        private void LogErrors(RouteSettingsContext context, IReadOnlyList<PatternCompilerError> errors)
        {
            foreach (var error in errors)
                LogError(context, error.ToString());
        }
    }
}
