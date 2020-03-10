namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using static Contract;
    using Options = OptionsProvider.ProxyOptions.UpstreamOptions.RequestOptions.HeadersOptions;

    internal sealed class RequestHeaderSetter : IRequestHeaderSetter
    {
        private static readonly HashSet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Headers.Names.ExternalAddress,
            HeaderNames.Host,
            Headers.Names.CallId,
            Headers.Names.ProxyName,
        };

        private readonly IRequestHeaderValuesProvider provider;
        private readonly ITraceIdProvider traceIdProvider;
        private readonly IHostProvider hostProvider;
        private readonly OptionsProvider options;
        private readonly ILogger<RequestHeaderSetter> logger;

        public RequestHeaderSetter(
            IRequestHeaderValuesProvider provider,
            ITraceIdProvider traceIdProvider,
            IHostProvider hostProvider,
            OptionsProvider options,
            ILogger<RequestHeaderSetter> logger)
        {
            this.provider = CheckValue(provider, nameof(provider));
            this.traceIdProvider = CheckValue(traceIdProvider, nameof(traceIdProvider));
            this.hostProvider = CheckValue(hostProvider, nameof(hostProvider));
            this.options = CheckValue(options, nameof(options));
            this.logger = CheckValue(logger, nameof(logger));
        }

        // Do NOT cache this value. Options react to changes.
        internal Options HeaderOptions => options.Proxy.Upstream.Request.Headers;

        public Task SetHeadersAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamRequest, nameof(upstreamRequest));

            context.RequestAborted.ThrowIfCancellationRequested();

            var correlationIdHeader = HeaderOptions.GetCorrelationIdHeader(context);
            AddDownstreamHeadersToUpstream(context, correlationIdHeader, upstreamRequest);
            AddHostHeader(context, upstreamRequest);
            AddExternalAddressHeader(context, upstreamRequest);
            AddClientAddressHeader(context, upstreamRequest);
            AddClientProtocolHeader(context, upstreamRequest);
            AddCorrelationIdHeader(context, correlationIdHeader, upstreamRequest);
            AddCallIdHeader(context, upstreamRequest);
            AddProxyNameHeader(context, upstreamRequest);
            AddExtraHeaders(context, upstreamRequest);

            return Task.CompletedTask;
        }

        private void AddDownstreamHeadersToUpstream(
            HttpContext context,
            string correlationIdHeader,
            HttpRequestMessage upstreamRequest)
        {
            if (HeaderOptions.IgnoreAllDownstreamRequestHeaders)
                return;

            var headers = context.Request.Headers;
            if (headers is null)
                return;

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
                if (HeaderOptions.HeaderNames.Contains(name))
                    continue;

                AddHeaderValues(context, upstreamRequest, name, header.Value);
            }
        }

        private void AddHostHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (HeaderOptions.IgnoreHost)
                return;

            upstreamRequest.Headers.TryAddWithoutValidation(
                HeaderNames.Host,
                hostProvider.GetHost(context));
        }

        private void AddExternalAddressHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (!HeaderOptions.IncludeExternalAddress)
                return;

            var clientAddress = GetRemoteIpAddressOrDefault(context);
            if (clientAddress is null)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Headers.Names.ExternalAddress,
                clientAddress);
        }

        private void AddClientAddressHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (HeaderOptions.IgnoreClientAddress)
                return;

            var clientAddress = GetRemoteIpAddressOrDefault(context);
            if (clientAddress is null)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Headers.Names.ClientAddress,
                clientAddress);
        }

        private void AddClientProtocolHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (HeaderOptions.IgnoreClientProtocol)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Headers.Names.ClientProtocol,
                context.Request.Scheme);
        }

        private void AddCorrelationIdHeader(HttpContext context, string correlationIdHeader, HttpRequestMessage upstreamRequest)
        {
            if (HeaderOptions.IgnoreCorrelationId)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                correlationIdHeader,
                traceIdProvider.GetCorrelationId(context));
        }

        private void AddCallIdHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (HeaderOptions.IgnoreCallId)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Headers.Names.CallId,
                traceIdProvider.GetCallId(context));
        }

        private void AddProxyNameHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (!HeaderOptions.TryGetProxyName(context, out var name))
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Headers.Names.ProxyName,
                name);
        }

        private void AddExtraHeaders(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            foreach (var header in HeaderOptions.Headers)
                upstreamRequest.Headers.TryAddWithoutValidation(header.Name, header.GetValues(context));
        }

        private void AddHeaderValues(
            HttpContext context,
            HttpRequestMessage upstreamRequest,
            string name,
            params string[] downstreamValues)
        {
            if (provider.TryGetHeaderValues(context, name, downstreamValues, out var upstreamValues) && upstreamValues != null)
            {
                upstreamRequest.Headers.TryAddWithoutValidation(name, upstreamValues);
                return;
            }

            logger.LogInformation(
                "Header '{0}' is not added. This was was instructed by the {1}.{2}.",
                name,
                nameof(IRequestHeaderValuesProvider),
                nameof(IRequestHeaderValuesProvider.TryGetHeaderValues));
        }

        private static string? GetRemoteIpAddressOrDefault(HttpContext context)
            => context.Connection.RemoteIpAddress?.ToString();
    }
}
