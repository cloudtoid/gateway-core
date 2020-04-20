namespace Cloudtoid.GatewayCore.Downstream
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore.Headers;
    using Microsoft.Extensions.Logging;
    using static Contract;

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
            ILogger<ResponseHeaderSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Logger = CheckValue(logger, nameof(logger));
            sanetizer = new HeaderSanetizer(logger);
        }

        protected IResponseHeaderValuesProvider Provider { get; }

        protected ILogger<ResponseHeaderSetter> Logger { get; }

        public virtual Task SetHeadersAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            cancellationToken.ThrowIfCancellationRequested();

            AddUpstreamResponseHeadersToDownstream(context, upstreamResponse);
            AddCorrelationIdHeader(context, upstreamResponse);
            AddCallIdHeader(context, upstreamResponse);
            AddExtraHeaders(context);

            return Task.CompletedTask;
        }

        protected virtual void AddUpstreamResponseHeadersToDownstream(
            ProxyContext context,
            HttpResponseMessage upstreamResponse)
        {
            var options = context.ProxyDownstreamResponseHeaderSettings;
            if (options.IgnoreAllUpstreamHeaders)
                return;

            if (upstreamResponse.Headers is null)
                return;

            var allowHeadersWithEmptyValue = options.AllowHeadersWithEmptyValue;
            var allowHeadersWithUnderscoreInName = options.AllowHeadersWithUnderscoreInName;
            var correlationIdHeader = context.CorrelationIdHeader;
            var headersWithOverride = options.OverrideNames;

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

        protected virtual void AddCorrelationIdHeader(ProxyContext context, HttpResponseMessage upstreamResponse)
        {
            if (!context.ProxyDownstreamResponseHeaderSettings.IncludeCorrelationId)
                return;

            AddHeaderValues(
                context,
                context.CorrelationIdHeader,
                context.CorrelationId);
        }

        protected virtual void AddCallIdHeader(ProxyContext context, HttpResponseMessage upstreamResponse)
        {
            if (!context.ProxyDownstreamResponseHeaderSettings.IncludeCallId)
                return;

            AddHeaderValues(
                context,
                Names.CallId,
                context.CallId);
        }

        protected virtual void AddExtraHeaders(ProxyContext context)
        {
            var headers = context.Response.Headers;

            foreach (var header in context.ProxyDownstreamResponseHeaderSettings.Overrides)
            {
                if (header.HasValues)
                    headers.AddOrAppendHeaderValues(header.Name, header.GetValues(context));
            }
        }

        protected virtual void AddHeaderValues(
            ProxyContext context,
            string name,
            params string[] upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.Headers.AddOrAppendHeaderValues(name, downstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by {1}.{2}.",
                name,
                nameof(IResponseHeaderValuesProvider),
                nameof(IResponseHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
