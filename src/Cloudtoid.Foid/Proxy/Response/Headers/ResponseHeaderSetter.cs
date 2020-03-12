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

    /// <summary>
    /// By inheriting from this clss, one can have full control over the outbound downstream response headers. Please consider the following extensibility points:
    /// 1. Inherit from <see cref="ResponseHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IResponseHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="ResponseHeaderSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IResponseHeaderSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IResponseHeaderValuesProvider, MyResponseHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IResponseHeaderSetter, MyResponseHeaderSetter>()</c>
    /// </summary>
    public class ResponseHeaderSetter : IResponseHeaderSetter
    {
        private static readonly ISet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ProxyHeaderNames.CallId,
        };

        public ResponseHeaderSetter(
            IResponseHeaderValuesProvider provider,
            ITraceIdProvider traceIdProvider,
            OptionsProvider options,
            ILogger<ResponseHeaderSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            TraceIdProvider = CheckValue(traceIdProvider, nameof(traceIdProvider));
            Options = CheckValue(options, nameof(options));
            Logger = CheckValue(logger, nameof(logger));
        }

        protected IResponseHeaderValuesProvider Provider { get; }

        protected ITraceIdProvider TraceIdProvider { get; }

        protected OptionsProvider Options { get; }

        protected ILogger<ResponseHeaderSetter> Logger { get; }

        // Do NOT cache this value. Options react to changes.
        private Options HeaderOptions => Options.Proxy.Downstream.Response.Headers;

        public virtual Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            AddUpstreamResponseHeadersToDownstream(context, upstreamResponse);
            AddCorrelationIdHeader(context, upstreamResponse);
            AddCallIdHeader(context, upstreamResponse);
            AddExtraHeaders(context);

            return Task.CompletedTask;
        }

        protected virtual void AddUpstreamResponseHeadersToDownstream(
            HttpContext context,
            HttpResponseMessage upstreamResponse)
        {
            if (HeaderOptions.IgnoreAllUpstreamResponseHeaders)
                return;

            var headers = upstreamResponse.Headers.ConcatOrEmpty(upstreamResponse.Content?.Headers);
            var correlationIdHeader = TraceIdProvider.GetCorrelationIdHeader(context);
            var headersWithOverride = HeaderOptions.HeaderNames;

            foreach (var header in headers)
            {
                var name = header.Key;

                // Remove empty headers
                if (!HeaderOptions.AllowHeadersWithEmptyValue && header.Value.All(s => string.IsNullOrEmpty(s)))
                {
                    Logger.LogInformation("Removing header '{0}' as its value is empty.", name);
                    continue;
                }

                // Remove headers with underscore in their names
                if (!HeaderOptions.AllowHeadersWithUnderscoreInName && name.Contains('_'))
                {
                    Logger.LogInformation("Removing header '{0}' as headers should not have underscores in their name.", header.Key);
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

        protected virtual void AddCorrelationIdHeader(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (!HeaderOptions.IncludeCorrelationId)
                return;

            AddHeaderValues(
                context,
                upstreamResponse,
                TraceIdProvider.GetCorrelationIdHeader(context),
                TraceIdProvider.GetCorrelationId(context));
        }

        protected virtual void AddCallIdHeader(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (!HeaderOptions.IncludeCallId)
                return;

            AddHeaderValues(
                context,
                upstreamResponse,
                ProxyHeaderNames.CallId,
                TraceIdProvider.GetCallId(context));
        }

        protected virtual void AddExtraHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            foreach (var header in HeaderOptions.Headers)
                headers.AddOrAppendHeaderValues(header.Name, header.GetValues(context));
        }

        protected virtual void AddHeaderValues(
            HttpContext context,
            HttpResponseMessage upstreamResponse,
            string nme,
            params string[] upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, nme, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.Headers.AddOrAppendHeaderValues(nme, downstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was was instructed by the {1}.{2}.",
                nme,
                nameof(IResponseHeaderValuesProvider),
                nameof(IResponseHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
