namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;
    using Options = OptionsProvider.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions;

    internal sealed class ResponseHeaderSetter : IResponseHeaderSetter
    {
        private static readonly ISet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Headers.Names.CallId,
        };

        private readonly IResponseHeaderValuesProvider provider;
        private readonly ITraceIdProvider traceIdProvider;
        private readonly OptionsProvider options;
        private readonly ILogger<ResponseHeaderSetter> logger;

        public ResponseHeaderSetter(
            IResponseHeaderValuesProvider provider,
            ITraceIdProvider traceIdProvider,
            OptionsProvider options,
            ILogger<ResponseHeaderSetter> logger)
        {
            this.provider = CheckValue(provider, nameof(provider));
            this.traceIdProvider = CheckValue(traceIdProvider, nameof(traceIdProvider));
            this.options = CheckValue(options, nameof(options));
            this.logger = CheckValue(logger, nameof(logger));
        }

        // Do NOT cache this value. Options react to changes.
        internal Options HeaderOptions => options.Proxy.Downstream.Response.Headers;

        public Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            AddUpstreamHeadersToDownstream(context, upstreamResponse);
            AddCorrelationIdHeader(context, upstreamResponse);
            AddCallIdHeader(context, upstreamResponse);
            AddExtraHeaders(context);

            return Task.CompletedTask;
        }

        private void AddUpstreamHeadersToDownstream(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (HeaderOptions.IgnoreAllUpstreamResponseHeaders)
                return;

            var headers = upstreamResponse.Headers;
            if (headers is null)
                return;

            var correlationIdHeader = traceIdProvider.GetCorrelationIdHeader(context);
            var headersWithOverride = HeaderOptions.HeaderNames;

            foreach (var header in headers)
            {
                var name = header.Key;

                // Remove empty headers
                if (!HeaderOptions.AllowHeadersWithEmptyValue && header.Value.All(s => string.IsNullOrEmpty(s)))
                {
                    logger.LogInformation("Removing header '{0}' as its value is empty.", name);
                    continue;
                }

                // Remove headers with underscore in their names
                if (!HeaderOptions.AllowHeadersWithUnderscoreInName && name.Contains('_'))
                {
                    logger.LogInformation("Removing header '{0}' as headers should not have underscores in their name.", header.Key);
                    continue;
                }

                if (name.EqualsOrdinalIgnoreCase(correlationIdHeader))
                    continue;

                // If blacklisted, we will not trasnfer its value
                if (HeaderTransferBlacklist.Contains(name))
                    continue;

                // If it has an override, we will not trasnfer its value
                if (headersWithOverride.Contains(name))
                    continue;

                AddHeaderValues(context, upstreamResponse, name, header.Value.AsArray());
            }
        }

        private void AddCorrelationIdHeader(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (!HeaderOptions.IncludeCorrelationId)
                return;

            AddHeaderValues(
                context,
                upstreamResponse,
                traceIdProvider.GetCorrelationIdHeader(context),
                traceIdProvider.GetCorrelationId(context));
        }

        private void AddCallIdHeader(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (!HeaderOptions.IncludeCallId)
                return;

            AddHeaderValues(
                context,
                upstreamResponse,
                Headers.Names.CallId,
                traceIdProvider.GetCallId(context));
        }

        private void AddExtraHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            foreach (var header in HeaderOptions.Headers)
                headers.AddOrAppendHeaderValues(header.Name, header.GetValues(context));
        }

        private void AddHeaderValues(
            HttpContext context,
            HttpResponseMessage upstreamResponse,
            string nme,
            params string[] upstreamValues)
        {
            if (provider.TryGetHeaderValues(context, nme, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.Headers.AddOrAppendHeaderValues(nme, downstreamValues);
                return;
            }

            logger.LogInformation(
                "Header '{0}' is not added. This was was instructed by the {1}.{2}.",
                nme,
                nameof(IResponseHeaderValuesProvider),
                nameof(IResponseHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
