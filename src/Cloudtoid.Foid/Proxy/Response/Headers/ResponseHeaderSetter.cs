namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Headers;
    using Cloudtoid.Foid.Options;
    using Cloudtoid.Foid.Trace;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;
    using Options = Options.OptionsProvider.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions;

    /// <summary>
    /// By inheriting from this class, one can have full control over the outbound downstream response headers. Please consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="ResponseHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="ResponseHeaderSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseHeaderSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseHeaderValuesProvider"/>, MyResponseHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseHeaderSetter"/>, MyResponseHeaderSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public class ResponseHeaderSetter : IResponseHeaderSetter
    {
        private static readonly ISet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Names.CallId,
        };

        private readonly HeaderSanetizer sanetizer;

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
            sanetizer = new HeaderSanetizer(logger);
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
            if (HeaderOptions.IgnoreAllUpstreamHeaders)
                return;

            if (upstreamResponse.Headers is null)
                return;

            var allowHeadersWithEmptyValue = HeaderOptions.AllowHeadersWithEmptyValue;
            var allowHeadersWithUnderscoreInName = HeaderOptions.AllowHeadersWithUnderscoreInName;
            var correlationIdHeader = TraceIdProvider.GetCorrelationIdHeader(context);
            var headersWithOverride = HeaderOptions.HeaderNames;

            foreach (var header in upstreamResponse.Headers)
            {
                var name = header.Key;

                if (!sanetizer.IsValid(
                    name,
                    header.Value,
                    allowHeadersWithEmptyValue,
                    allowHeadersWithUnderscoreInName))
                    continue;

                if (name.EqualsOrdinalIgnoreCase(correlationIdHeader))
                    continue;

                // If blacklisted, we will not transfer its value
                if (HeaderTransferBlacklist.Contains(name))
                    continue;

                // If it has an override, we will not transfer its value
                if (headersWithOverride.Contains(name))
                    continue;

                AddHeaderValues(context, name, header.Value.AsArray());
            }
        }

        protected virtual void AddCorrelationIdHeader(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (!HeaderOptions.IncludeCorrelationId)
                return;

            AddHeaderValues(
                context,
                TraceIdProvider.GetCorrelationIdHeader(context),
                TraceIdProvider.GetCorrelationId(context));
        }

        protected virtual void AddCallIdHeader(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (!HeaderOptions.IncludeCallId)
                return;

            AddHeaderValues(
                context,
                Names.CallId,
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
            string name,
            params string[] upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.Headers.AddOrAppendHeaderValues(name, downstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by the {1}.{2}.",
                name,
                nameof(IResponseHeaderValuesProvider),
                nameof(IResponseHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
