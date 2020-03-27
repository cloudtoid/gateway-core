namespace Cloudtoid.Foid.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cloudtoid.Foid.Expression;
    using Microsoft.Extensions.Logging;
    using static Cloudtoid.Foid.ReverseProxyOptions;
    using static Cloudtoid.Foid.ReverseProxyOptions.RouteOptions;
    using static Cloudtoid.Foid.ReverseProxyOptions.RouteOptions.ProxyOptions;
    using static Contract;

    internal sealed class SettingsCreator : ISettingsCreator
    {
        private readonly IExpressionEvaluator evaluator;
        private readonly ILogger<SettingsCreator> logger;

        public SettingsCreator(
            IExpressionEvaluator evaluator,
            ILogger<SettingsCreator> logger)
        {
            this.evaluator = CheckValue(evaluator, nameof(evaluator));
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

        private RouteSettings? Create(string route, RouteOptions options)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                LogError("A route cannot be an empty string.");
                return null;
            }

            var context = new RouteSettingsContext(route, evaluator);

            return new RouteSettings(
                route,
                Create(context, options.Proxy));
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
            => logger.LogError($"{nameof(ReverseProxyOptions)} error: {message}");

        private void LogError(RouteSettingsContext context, string message)
            => logger.LogError($"{nameof(ReverseProxyOptions)} ({context.Route}) error: {message}");

        private void LogWarning(RouteSettingsContext context, string message)
            => logger.LogWarning($"{nameof(ReverseProxyOptions)} warning: ({context.Route}) {message}");
    }
}
