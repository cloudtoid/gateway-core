namespace Cloudtoid.Foid.Routes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Cloudtoid.Foid.Expression;
    using Cloudtoid.Foid.Options;
    using Microsoft.Extensions.Logging;
    using static Cloudtoid.Foid.Options.FoidOptions;
    using static Cloudtoid.Foid.Options.FoidOptions.RouteOptions;
    using static Cloudtoid.Foid.Options.FoidOptions.RouteOptions.ProxyOptions;
    using static Cloudtoid.Foid.Routes.RouteSettings;
    using static Cloudtoid.Foid.Routes.RouteSettings.ProxySettings;
    using static Cloudtoid.Foid.Routes.RouteSettings.ProxySettings.UpstreamRequestSettings;
    using static Contract;

    internal sealed class RouteSettingsCreator : IRouteSettingsCreator
    {
        private readonly IExpressionEvaluator evaluator;
        private readonly ILogger<RouteSettingsCreator> logger;

        public RouteSettingsCreator(
            IExpressionEvaluator evaluator,
            ILogger<RouteSettingsCreator> logger)
        {
            this.evaluator = CheckValue(evaluator, nameof(evaluator));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public bool TryCreate(
           string route,
           RouteOptions options,
           [NotNullWhen(true)] out RouteSettings? result)
        {
            result = Create(route, options);
            return result != null;
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

        private HeadersSettings Create(
            RouteSettingsContext context,
            UpstreamRequestOptions.HeadersOptions options)
        {
            return new HeadersSettings(
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

        private SenderSettings Create(
            UpstreamRequestOptions.SenderOptions options)
        {
            return new SenderSettings(
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

        private DownstreamResponseSettings.HeadersSettings Create(
            RouteSettingsContext context,
            DownstreamResponseOptions.HeadersOptions options)
        {
            return new DownstreamResponseSettings.HeadersSettings(
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
            => logger.LogError($"{nameof(FoidOptions)} error: {message}");

        private void LogError(RouteSettingsContext context, string message)
            => logger.LogError($"{nameof(FoidOptions)} ({context.Route}) error: {message}");

        private void LogWarning(RouteSettingsContext context, string message)
            => logger.LogWarning($"{nameof(FoidOptions)} warning: ({context.Route}) {message}");
    }
}
