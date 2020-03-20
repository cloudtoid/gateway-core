namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Headers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using static Contract;

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
    public class RequestHeaderSetter : IRequestHeaderSetter
    {
        private static readonly ISet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Host,
            Names.ExternalAddress,
            Names.ProxyName,
            Names.CallId,
            Names.ForwardedFor,
            Names.ForwardedHost,
            Names.ForwardedProtocol
        };

        private readonly HeaderSanetizer sanetizer;

        public RequestHeaderSetter(
            IRequestHeaderValuesProvider provider,
            ILogger<RequestHeaderSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Logger = CheckValue(logger, nameof(logger));
            sanetizer = new HeaderSanetizer(logger);
        }

        protected IRequestHeaderValuesProvider Provider { get; }

        protected ILogger<RequestHeaderSetter> Logger { get; }

        public virtual Task SetHeadersAsync(
            CallContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamRequest, nameof(upstreamRequest));

            cancellationToken.ThrowIfCancellationRequested();

            AddDownstreamRequestHeadersToUpstream(context, upstreamRequest);
            AddHostHeader(context, upstreamRequest);
            AddExternalAddressHeader(context, upstreamRequest);
            AddForwardedForHeader(context, upstreamRequest);
            AddForwardedProtocolHeader(context, upstreamRequest);
            AddForwardedHostHeader(context, upstreamRequest);
            AddCorrelationIdHeader(context, upstreamRequest);
            AddCallIdHeader(context, upstreamRequest);
            AddProxyNameHeader(context, upstreamRequest);
            AddExtraHeaders(context, upstreamRequest);

            return Task.CompletedTask;
        }

        protected virtual void AddDownstreamRequestHeadersToUpstream(
            CallContext context,
            HttpRequestMessage upstreamRequest)
        {
            var options = context.ProxyUpstreamRequestHeadersOptions;
            if (options.IgnoreAllDownstreamHeaders)
                return;

            var headers = context.Request.Headers;
            if (headers is null)
                return;

            var allowHeadersWithEmptyValue = options.AllowHeadersWithEmptyValue;
            var allowHeadersWithUnderscoreInName = options.AllowHeadersWithUnderscoreInName;
            var correlationIdHeader = context.CorrelationIdHeader;
            var headersWithOverride = options.HeaderNames;

            foreach (var header in headers)
            {
                var name = header.Key;

                if (!sanetizer.IsValid(
                    name,
                    header.Value,
                    allowHeadersWithEmptyValue,
                    allowHeadersWithUnderscoreInName))
                    continue;

                // Content headers should not be here. Ignore them.
                if (HeaderTypes.IsContentHeader(name))
                    continue;

                if (name.EqualsOrdinalIgnoreCase(correlationIdHeader))
                    continue;

                // If blacklisted, we will not transfer its value
                if (HeaderTransferBlacklist.Contains(name))
                    continue;

                // If it has an override, we will not transfer its value
                if (headersWithOverride.Contains(name))
                    continue;

                AddHeaderValues(context, upstreamRequest, name, header.Value);
            }
        }

        protected virtual void AddHostHeader(CallContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersOptions.IgnoreHost)
                return;

            upstreamRequest.Headers.TryAddWithoutValidation(
                HeaderNames.Host,
                context.Host);
        }

        protected virtual void AddExternalAddressHeader(CallContext context, HttpRequestMessage upstreamRequest)
        {
            if (!context.ProxyUpstreamRequestHeadersOptions.IncludeExternalAddress)
                return;

            var clientAddress = GetRemoteIpAddressOrDefault(context);
            if (clientAddress is null)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ExternalAddress,
                clientAddress);
        }

        protected virtual void AddForwardedForHeader(CallContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersOptions.IgnoreForwardedFor)
                return;

            var clientAddress = GetRemoteIpAddressOrDefault(context);
            if (clientAddress is null)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ForwardedFor,
                clientAddress);
        }

        protected virtual void AddForwardedProtocolHeader(CallContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersOptions.IgnoreForwardedProtocol)
                return;

            if (string.IsNullOrEmpty(context.Request.Scheme))
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ForwardedProtocol,
                context.Request.Scheme);
        }

        protected virtual void AddForwardedHostHeader(CallContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersOptions.IgnoreForwardedHost)
                return;

            var host = context.Request.Host;
            if (!host.HasValue)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ForwardedHost,
                host.Value);
        }

        protected virtual void AddCorrelationIdHeader(CallContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersOptions.IgnoreCorrelationId)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                context.CorrelationIdHeader,
                context.CorrelationId);
        }

        protected virtual void AddCallIdHeader(CallContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersOptions.IgnoreCallId)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.CallId,
                context.CallId);
        }

        protected virtual void AddProxyNameHeader(CallContext context, HttpRequestMessage upstreamRequest)
        {
            if (!context.ProxyUpstreamRequestHeadersOptions.TryGetProxyName(context, out var name))
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ProxyName,
                name);
        }

        protected virtual void AddExtraHeaders(CallContext context, HttpRequestMessage upstreamRequest)
        {
            foreach (var header in context.ProxyUpstreamRequestHeadersOptions.Headers)
                upstreamRequest.Headers.TryAddWithoutValidation(header.Name, header.GetValues(context));
        }

        protected virtual void AddHeaderValues(
            CallContext context,
            HttpRequestMessage upstreamRequest,
            string name,
            params string[] downstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, downstreamValues, out var upstreamValues) && upstreamValues != null)
            {
                upstreamRequest.Headers.TryAddWithoutValidation(name, upstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by the {1}.{2}.",
                name,
                nameof(IRequestHeaderValuesProvider),
                nameof(IRequestHeaderValuesProvider.TryGetHeaderValues));
        }

        private static string? GetRemoteIpAddressOrDefault(CallContext context)
        {
            var address = context.HttpContext.Connection.RemoteIpAddress;
            if (address is null)
                return null;

            var add = address.ToString();
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
                return "\\" + add + "\"";

            return add;
        }
    }
}
