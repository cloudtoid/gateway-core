﻿namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    internal sealed class RequestHeaderSetter : IRequestHeaderSetter
    {
        private readonly IRequestHeaderValuesProvider provider;
        private readonly IGuidProvider guideProvider;
        private readonly ILogger<RequestHeaderSetter> logger;

        public RequestHeaderSetter(
            IRequestHeaderValuesProvider provider,
            IGuidProvider guideProvider,
            ILogger<RequestHeaderSetter> logger)
        {
            this.provider = CheckValue(provider, nameof(provider));
            this.guideProvider = CheckValue(guideProvider, nameof(guideProvider));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public Task SetHeadersAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamRequest, nameof(upstreamRequest));

            context.RequestAborted.ThrowIfCancellationRequested();

            var headers = context.Request.Headers;
            if (headers is null)
                return Task.CompletedTask;

            foreach (var header in headers)
            {
                // Remove empty headers
                if (!provider.AllowHeadersWithEmptyValue && header.Value.All(s => string.IsNullOrEmpty(s)))
                {
                    logger.LogInformation("Removing header '{0}' as its value is empty.", header.Key);
                    continue;
                }

                // Remove headers with underscore in their names
                if (!provider.AllowHeadersWithUnderscoreInName && header.Key.Contains('_'))
                {
                    logger.LogInformation("Removing header '{0}' as headers should not have underscores in their name.", header.Key);
                    continue;
                }

                if (!provider.TryGetHeaderValues(context, header.Key, header.Value, out var upstreamValues) || upstreamValues is null)
                {
                    logger.LogInformation(
                        "Removing header '{0}' as was instructed by the {1}.",
                        header.Key,
                        nameof(IRequestHeaderValuesProvider));

                    continue;
                }

                upstreamRequest.Headers.TryAddWithoutValidation(header.Key, upstreamValues);
            }

            AddHostHeader(context, upstreamRequest);
            AddExternalAddressHeader(context, upstreamRequest);
            AddClientAddressHeader(context, upstreamRequest);
            AddClientProtocolHeader(context, upstreamRequest);
            AddRequestIdHeader(context, upstreamRequest);
            AddCallIdHeader(upstreamRequest);
            AddProxyNameHeader(context, upstreamRequest);
            AddExtraHeaders(context, upstreamRequest);

            return Task.CompletedTask;
        }

        private void AddHostHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.Request.Headers.ContainsKey(HeaderNames.Host))
                return;

            upstreamRequest.Headers.TryAddWithoutValidation(
                HeaderNames.Host,
                provider.GetDefaultHostHeaderValue(context));
        }

        private void AddExternalAddressHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (!provider.IncludeExternalAddress)
                return;

            var clientAddress = GetRemoteIpAddressOrDefault(context);
            if (clientAddress is null)
                return;

            upstreamRequest.Headers.TryAddWithoutValidation(
                Request.Constants.Headers.ExternalAddress,
                clientAddress);
        }

        private void AddClientAddressHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (provider.IgnoreClientAddress)
                return;

            var clientAddress = GetRemoteIpAddressOrDefault(context);
            if (clientAddress is null)
                return;

            if (context.Request.Headers.TryGetValue(Request.Constants.Headers.ClientAddress, out var values))
            {
                values = StringValues.Concat(values, clientAddress);
            }
            else
            {
                values = clientAddress;
            }

            upstreamRequest.Headers.TryAddWithoutValidation(
                Request.Constants.Headers.ClientAddress,
                (IEnumerable<string>)values);
        }

        private void AddClientProtocolHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (provider.IgnoreClientProtocol)
                return;

            upstreamRequest.Headers.TryAddWithoutValidation(
                Request.Constants.Headers.ClientProtocol,
                context.Request.Scheme);
        }

        private void AddRequestIdHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (!provider.IgnoreRequestId && !context.Request.Headers.ContainsKey(Request.Constants.Headers.RequestId))
            {
                upstreamRequest.Headers.TryAddWithoutValidation(
                    Request.Constants.Headers.RequestId,
                    guideProvider.NewGuid().ToStringInvariant("N"));
            }
        }

        private void AddCallIdHeader(HttpRequestMessage upstreamRequest)
        {
            if (provider.IgnoreCallId)
                return;

            upstreamRequest.Headers.TryAddWithoutValidation(
                Request.Constants.Headers.CallId,
                guideProvider.NewGuid().ToStringInvariant("N"));
        }

        private void AddProxyNameHeader(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            var name = provider.GetProxyNameHeaderValue(context);
            if (string.IsNullOrWhiteSpace(name))
                return;

            upstreamRequest.Headers.TryAddWithoutValidation(
                Request.Constants.Headers.ProxyName,
                name);
        }

        private void AddExtraHeaders(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            foreach (var header in provider.GetExtraHeaders(context))
                upstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Values);
        }

        private static string? GetRemoteIpAddressOrDefault(HttpContext context)
            => context.Connection.RemoteIpAddress?.ToString();
    }
}
