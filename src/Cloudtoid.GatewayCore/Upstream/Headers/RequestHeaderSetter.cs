using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cloudtoid.GatewayCore.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Upstream
{
    /// <summary>
    /// By inheriting from this class, one can have full control over the outbound upstream request headers. Please consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="RequestHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="RequestHeaderSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestHeaderSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestHeaderValuesProvider"/>, MyRequestHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestHeaderSetter"/>, MyRequestHeaderSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public partial class RequestHeaderSetter : IRequestHeaderSetter
    {
        private readonly HeaderSanitizer sanitizer;

        public RequestHeaderSetter(
            IRequestHeaderValuesProvider provider,
            ILogger<RequestHeaderSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Logger = CheckValue(logger, nameof(logger));
            sanitizer = new HeaderSanitizer(logger);
        }

        protected IRequestHeaderValuesProvider Provider { get; }

        protected ILogger<RequestHeaderSetter> Logger { get; }

        public virtual Task SetHeadersAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamRequest, nameof(upstreamRequest));

            cancellationToken.ThrowIfCancellationRequested();

            var settings = context.ProxyUpstreamRequestHeadersSettings;

            if (!settings.DiscardInboundHeaders)
                AddDownstreamRequestHeadersToUpstream(context, upstreamRequest);

            if (!settings.SkipVia)
                AddViaHeader(context, upstreamRequest);

            if (settings.AddExternalAddress)
                AddExternalAddressHeader(context, upstreamRequest);

            if (settings.AddProxyName)
                AddProxyNameHeader(context, upstreamRequest);

            if (!settings.SkipForwarded)
                AddForwardedHeaders(context, upstreamRequest);

            AddExtraHeaders(context, upstreamRequest);

            return Task.CompletedTask;
        }

        protected virtual void AddDownstreamRequestHeadersToUpstream(
            ProxyContext context,
            HttpRequestMessage upstreamRequest)
        {
            var options = context.ProxyUpstreamRequestHeadersSettings;
            var headers = context.Request.Headers;
            var discardEmpty = options.DiscardEmpty;
            var discardUnderscore = options.DiscardUnderscore;
            var doNotTransferHeaders = options.DoNotTransferHeaders;
            ISet<string>? nonStandardHopByHopHeaders = null; // lazy instantiate if and only if needed

            foreach (var header in headers)
            {
                var name = header.Key;

                if (!sanitizer.IsValid(
                    name,
                    header.Value,
                    discardEmpty,
                    discardUnderscore))
                    continue;

                if (doNotTransferHeaders.Contains(name))
                    continue;

                if (nonStandardHopByHopHeaders is null)
                    nonStandardHopByHopHeaders = GetNonStandardHopByHopHeaders(headers);

                if (nonStandardHopByHopHeaders.Contains(name))
                    continue;

                AddHeaderValues(context, upstreamRequest, name, header.Value);
            }
        }

        protected virtual void AddExternalAddressHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var clientAddress = GetRemoteIpAddressOrDefault(context);
            if (clientAddress is null)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ExternalAddress,
                clientAddress);
        }

        protected virtual void AddProxyNameHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ProxyName,
                context.ProxyName);
        }

        protected virtual void AddExtraHeaders(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var headers = upstreamRequest.Headers;

            foreach (var (name, header) in context.ProxyUpstreamRequestHeadersSettings.Overrides)
                headers.TryAddWithoutValidation(name, header.EvaluateValues(context));

            foreach (var (name, header) in context.ProxyUpstreamRequestHeadersSettings.Appends)
                headers.TryAddWithoutValidation(name, header.EvaluateValues(context));
        }

        protected virtual void AddHeaderValues(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            string name,
            StringValues downstreamValues)
        {
            if (!Provider.TryGetHeaderValues(context, name, downstreamValues, out var upstreamValues))
            {
                Logger.LogInformation(
                    "Header '{0}' is not added. This was instructed by {1}.{2}.",
                    name,
                    nameof(IRequestHeaderValuesProvider),
                    nameof(IRequestHeaderValuesProvider.TryGetHeaderValues));

                return;
            }

            // Perf: for perf reasons, we are casting below.
            if (StringValues.IsNullOrEmpty(upstreamValues))
            {
                if (!upstreamRequest.Headers.TryAddWithoutValidation(name, string.Empty) && upstreamRequest.Content is not null)
                    upstreamRequest.Content.Headers.TryAddWithoutValidation(name, string.Empty);
            }
            else if (upstreamValues.Count == 1)
            {
                var value = (string)upstreamValues;
                if (!upstreamRequest.Headers.TryAddWithoutValidation(name, value) && upstreamRequest.Content is not null)
                    upstreamRequest.Content.Headers.TryAddWithoutValidation(name, value);
            }
            else if (upstreamValues.Count > 1)
            {
                var values = (string[])upstreamValues;
                if (!upstreamRequest.Headers.TryAddWithoutValidation(name, values) && upstreamRequest.Content is not null)
                    upstreamRequest.Content.Headers.TryAddWithoutValidation(name, values);
            }
        }

        private static string? GetRemoteIpAddressOrDefault(ProxyContext context)
            => context.HttpContext.Connection.RemoteIpAddress?.ToString();

        /// <summary>
        /// Except for the standard hop-by-hop headers (Keep-Alive, Transfer-Encoding, TE, Connection, Trailer, Upgrade,
        /// Proxy-Authorization and Proxy-Authenticate), any hop-by-hop headers used by the message must be listed in the
        /// Connection header, so that the first proxy knows it has to consume them and not forward them further.
        /// Standard hop-by-hop headers can be listed too (it is often the case of Keep-Alive, but this is not mandatory).
        /// See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Connection">here</a> for more information.
        /// </summary>
        private static ISet<string> GetNonStandardHopByHopHeaders(IHeaderDictionary headers)
        {
            if (!headers.ContainsKey(HeaderNames.Connection))
                return ImmutableHashSet<string>.Empty;

            var values = headers.GetCommaSeparatedValues(HeaderNames.Connection);
            if (values.Length == 0)
                return ImmutableHashSet<string>.Empty;

            return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
        }
    }
}
