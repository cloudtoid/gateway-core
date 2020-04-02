namespace Cloudtoid.Foid.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cloudtoid.Foid.Expression;
    using Cloudtoid.UrlPattern;
    using Microsoft.Extensions.Logging;
    using static Cloudtoid.Foid.ReverseProxyOptions;
    using static Cloudtoid.Foid.ReverseProxyOptions.RouteOptions;
    using static Cloudtoid.Foid.ReverseProxyOptions.RouteOptions.ProxyOptions;
    using static Contract;

    internal sealed class SettingsCreator : ISettingsCreator
    {
        private readonly IExpressionEvaluator evaluator;
        private readonly IPatternEngine patternEngine;
        private readonly ILogger<SettingsCreator> logger;

        public SettingsCreator(
            IExpressionEvaluator evaluator,
            IPatternEngine patternEngine,
            ILogger<SettingsCreator> logger)
        {
            this.evaluator = CheckValue(evaluator, nameof(evaluator));
            this.patternEngine = CheckValue(patternEngine, nameof(patternEngine));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public ReverseProxySettings Create(ReverseProxyOptions options)
        {
            var routes = options.Routes
                .Select(o => Create(o.Key, o.Value))
                .WhereNotNull()
                .ToArray();

            if (routes.Length == 0)
                LogError("No routes are specified.");

            return new ReverseProxySettings(routes);
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

            var correlationIdHeader = string.IsNullOrWhiteSpace(options.CorrelationIdHeader) ? null : options.CorrelationIdHeader;

            return new ProxySettings(
                context,
                options.To,
                correlationIdHeader,
                Create(context, options.UpstreamRequest),
                Create(context, options.DownstreamResponse));
        }

        private UpstreamRequestSettings Create(
            RouteSettingsContext context,
            UpstreamRequestOptions options)
        {
            return new UpstreamRequestSettings(
                context,
                options.HttpVersion,
                options.TimeoutInMilliseconds,
                Create(context, options.Headers),
                Create(options.Sender));
        }

        private UpstreamRequestHeadersSettings Create(
            RouteSettingsContext context,
            UpstreamRequestOptions.HeadersOptions options)
        {
            return new UpstreamRequestHeadersSettings(
                context,
                options.DefaultHost,
                options.ProxyName,
                options.AllowHeadersWithEmptyValue,
                options.AllowHeadersWithUnderscoreInName,
                options.IncludeExternalAddress,
                options.IgnoreAllDownstreamHeaders,
                options.IgnoreHost,
                options.IgnoreForwardedFor,
                options.IgnoreForwardedProtocol,
                options.IgnoreForwardedHost,
                options.IgnoreCorrelationId,
                options.IgnoreCallId,
                Create(context, options.Overrides));
        }

        private UpstreamRequestSenderSettings Create(
            UpstreamRequestOptions.SenderOptions options)
        {
            return new UpstreamRequestSenderSettings(
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
