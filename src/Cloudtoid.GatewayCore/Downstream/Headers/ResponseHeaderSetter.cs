using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cloudtoid.GatewayCore.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Downstream
{
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
        private const string WildcardCookieName = "*";
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

            var settings = context.ProxyDownstreamResponseHeaderSettings;

            if (!settings.DiscardInboundHeaders)
                AddUpstreamResponseHeadersToDownstream(context, upstreamResponse);

            if (!settings.SkipVia)
                AddViaHeader(context, upstreamResponse);

            if (settings.AddCorrelationId)
                AddCorrelationIdHeader(context, upstreamResponse);

            if (settings.AddCallId)
                AddCallIdHeader(context, upstreamResponse);

            if (settings.AddServer)
                AddServerHeader(context, upstreamResponse);

            AddExtraHeaders(context);

            return Task.CompletedTask;
        }

        protected virtual void AddUpstreamResponseHeadersToDownstream(
            ProxyContext context,
            HttpResponseMessage upstreamResponse)
        {
            if (upstreamResponse.Headers is null)
                return;

            var options = context.ProxyDownstreamResponseHeaderSettings;
            var discardEmpty = options.DiscardEmpty;
            var discardUnderscore = options.DiscardUnderscore;
            var correlationIdHeader = context.CorrelationIdHeader;
            var doNotTransferHeaders = options.DoNotTransferHeaders;

            foreach (var header in upstreamResponse.Headers)
            {
                var name = header.Key;

                if (!sanetizer.IsValid(
                    name,
                    header.Value,
                    discardEmpty,
                    discardUnderscore))
                    continue;

                if (name.EqualsOrdinalIgnoreCase(correlationIdHeader))
                    continue;

                if (doNotTransferHeaders.Contains(name))
                    continue;

                var values = header.Value.AsStringValues();

                if (name.EqualsOrdinalIgnoreCase(HeaderNames.SetCookie))
                    UpdateSetCookiesValues(context, values);

                AddHeaderValues(context, name, values);
            }
        }

        protected virtual void UpdateSetCookiesValues(ProxyContext context, string[] values)
        {
            if (context.ProxyDownstreamResponseHeaderSettings.Cookies.Count == 0)
                return;

            var len = values.Length;
            for (int i = 0; i < len; i++)
                values[i] = GetSetCookiesValue(context, values[i]);
        }

        protected static string GetSetCookiesValue(ProxyContext context, string value)
        {
            if (!SetCookieHeaderValue.TryParse(value, out var cookie) || !cookie.Name.HasValue)
                return value;

            var cookieSettings = context.ProxyDownstreamResponseHeaderSettings.Cookies;
            if (!cookieSettings.TryGetValue(cookie.Name.Value, out var cookieSetting) &&
                !cookieSettings.TryGetValue(WildcardCookieName, out cookieSetting))
                return value;

            if (cookieSetting.SameSite != Microsoft.Net.Http.Headers.SameSiteMode.Unspecified)
                cookie.SameSite = cookieSetting.SameSite;

            if (cookieSetting.Secure.HasValue)
                cookie.Secure = cookieSetting.Secure.Value;

            if (cookieSetting.HttpOnly.HasValue)
                cookie.HttpOnly = cookieSetting.HttpOnly.Value;

            var domain = cookieSetting.EvaluateDomain(context);
            if (domain is not null)
                cookie.Domain = domain.Length == 0 ? null : domain;

            return cookie.ToString();
        }

        /// <summary>
        /// The Via general header is added by proxies, both forward and reverse proxies, and can appear in
        /// the request headers and the response headers. It is used for tracking message forwards, avoiding
        /// request loops, and identifying the protocol capabilities of senders along the request/response chain.
        /// See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via">here</a> for more information.
        /// </summary>
        protected virtual void AddViaHeader(ProxyContext context, HttpResponseMessage upstreamResponse)
        {
            IEnumerable<string>? values = null;
            if (!context.ProxyDownstreamResponseHeaderSettings.DiscardInboundHeaders)
                upstreamResponse.Headers.TryGetValues(HeaderNames.Via, out values);

            var version = upstreamResponse.Version;
            var value = $"{version.Major}.{version.Minor} {context.ProxyName}";

            AddHeaderValues(
                context,
                HeaderNames.Via,
                values.Concat(value).AsStringValues());
        }

        protected virtual void AddServerHeader(ProxyContext context, HttpResponseMessage upstreamResponse)
        {
            AddHeaderValues(
                context,
                HeaderNames.Server,
                Constants.ServerName);
        }

        protected virtual void AddCorrelationIdHeader(ProxyContext context, HttpResponseMessage upstreamResponse)
        {
            AddHeaderValues(
                context,
                context.CorrelationIdHeader,
                context.CorrelationId);
        }

        protected virtual void AddCallIdHeader(ProxyContext context, HttpResponseMessage upstreamResponse)
        {
            AddHeaderValues(
                context,
                Names.CallId,
                context.CallId);
        }

        protected virtual void AddExtraHeaders(ProxyContext context)
        {
            var headers = context.Response.Headers;

            foreach (var (name, header) in context.ProxyDownstreamResponseHeaderSettings.Overrides)
                headers.Append(name, header.EvaluateValues(context).AsStringValues());

            foreach (var (name, header) in context.ProxyDownstreamResponseHeaderSettings.Appends)
                headers.Append(name, header.EvaluateValues(context).AsStringValues());
        }

        protected virtual void AddHeaderValues(
            ProxyContext context,
            string name,
            StringValues upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, upstreamValues, out var downstreamValues) && downstreamValues.Count > 0)
            {
                context.Response.Headers.Append(name, downstreamValues);
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
