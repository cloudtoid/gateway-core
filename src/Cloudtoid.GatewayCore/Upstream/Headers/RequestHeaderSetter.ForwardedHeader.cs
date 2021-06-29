using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Cloudtoid.GatewayCore.Headers;
using Microsoft.AspNetCore.Http;

namespace Cloudtoid.GatewayCore.Upstream
{
    public partial class RequestHeaderSetter
    {
        private const string ForwardedBy = "by=";
        private const string ForwardedFor = "for=";
        private const string ForwardedProto = "proto=";
        private const string ForwardedHost = "host=";
        private const string CommaAndSpace = ", ";
        private const char Semicolon = ';';
        private static readonly HashSet<string> ValidWellKnownForwardedIdentifers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "_hidden",
            "_secret",
            "unknown"
        };

        protected virtual void AddForwardedHeaders(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersSettings.UseXForwarded)
                AddXForwardedHeaders(context, upstreamRequest);
            else
                AddForwardedHeader(context, upstreamRequest);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded
        protected virtual void AddForwardedHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var latestValue = CreateLatestForwardHeaderValue(context);

            var value = context.ProxyUpstreamRequestHeadersSettings.DiscardInboundHeaders
                ? CreateForwardHeaderValue(latestValue)
                : CreateForwardHeaderValue(GetCurrentForwardedHeaderValues(context.Request.Headers).Concat(latestValue));

            if (value is null)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.Forwarded,
                value);
        }

        private void AddXForwardedHeaders(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var forwardedHeaderValues =
                context.ProxyUpstreamRequestHeadersSettings.DiscardInboundHeaders
                ? ReadOnlyValueList<ForwardedHeaderValue>.Empty
                : new ReadOnlyValueList<ForwardedHeaderValue>(GetCurrentForwardedHeaderValues(context.Request.Headers));

            AddXForwardedForHeader(context, upstreamRequest, in forwardedHeaderValues);
            AddXForwardedProtocolHeader(context, upstreamRequest, in forwardedHeaderValues);
            AddXForwardedHostHeader(context, upstreamRequest, in forwardedHeaderValues);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-For
        protected virtual void AddXForwardedForHeader(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            in ReadOnlyValueList<ForwardedHeaderValue> forwardedHeaderValues)
        {
            var @for = CreateXForwardedForHeader(context, in forwardedHeaderValues);
            if (@for is null)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.XForwardedFor,
                @for);
        }

        private static string? CreateXForwardedForHeader(
            ProxyContext context,
            in ReadOnlyValueList<ForwardedHeaderValue> forwardedHeaderValues)
        {
            var result = GetRemoteIpAddressOrDefault(context);
            if (string.IsNullOrEmpty(result))
                return null;

            if (forwardedHeaderValues.Count == 0)
                return result;

            StringBuilder? builder = null;

            foreach (var value in forwardedHeaderValues)
            {
                var @for = value.For;
                if (!string.IsNullOrEmpty(@for))
                {
                    if (builder is null)
                        builder = new StringBuilder();
                    else
                        builder.Append(CommaAndSpace);

                    if (TryParseIpV6Address(@for, out var ip))
                        @for = ip.ToString();

                    builder.Append(@for);
                }
            }

            if (builder is not null)
                result = builder.Append(CommaAndSpace).Append(result).ToString();

            return result;
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Proto
        protected virtual void AddXForwardedProtocolHeader(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            in ReadOnlyValueList<ForwardedHeaderValue> forwardedHeaderValues)
        {
            var proto = forwardedHeaderValues.FirstOrDefault(v => !string.IsNullOrEmpty(v.Proto)).Proto ?? context.Request.Scheme;
            if (string.IsNullOrEmpty(proto))
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.XForwardedProto,
                proto);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Host
        protected virtual void AddXForwardedHostHeader(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            in ReadOnlyValueList<ForwardedHeaderValue> forwardedHeaderValues)
        {
            var host = forwardedHeaderValues.FirstOrDefault(v => !string.IsNullOrEmpty(v.Host)).Host ?? context.Request.Host.Value;
            if (string.IsNullOrEmpty(host))
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.XForwardedHost,
                host);
        }

        private static ForwardedHeaderValue CreateLatestForwardHeaderValue(ProxyContext context)
        {
            return new ForwardedHeaderValue(
                by: CreateValidForwardedIpAddress(context.HttpContext.Connection.LocalIpAddress),
                @for: CreateValidForwardedIpAddress(context.HttpContext.Connection.RemoteIpAddress),
                host: context.Request.Host.Value,
                proto: context.Request.Scheme);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded
        private static string? CreateForwardHeaderValue(IEnumerable<ForwardedHeaderValue> values)
        {
            StringBuilder? builder = null;
            foreach (var value in values)
                AppendForwardHeaderValue(ref builder, value);

            return builder?.ToString();
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded
        private static string? CreateForwardHeaderValue(ForwardedHeaderValue value)
        {
            StringBuilder? builder = null;
            AppendForwardHeaderValue(ref builder, value);
            return builder?.ToString();
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded
        private static void AppendForwardHeaderValue(ref StringBuilder? builder, ForwardedHeaderValue value)
        {
            var hasBy = !string.IsNullOrEmpty(value.By);
            var hasFor = !string.IsNullOrEmpty(value.For);
            var hasHost = !string.IsNullOrEmpty(value.Host);
            var hasProto = !string.IsNullOrEmpty(value.Proto);

            if (!hasBy && !hasFor && !hasHost && !hasProto)
                return;

            bool appendComma;
            if (builder == null)
            {
                builder = new StringBuilder();
                appendComma = false;
            }
            else
            {
                appendComma = true;
            }

            if (hasBy)
            {
                AppendSeperator(builder);
                builder.Append(ForwardedBy);
                builder.Append(GetValidForwardedIdentifier(value.By!));
            }

            if (hasFor)
            {
                AppendSeperator(builder);
                builder.Append(ForwardedFor);
                builder.Append(GetValidForwardedIdentifier(value.For!));
            }

            if (hasHost)
            {
                AppendSeperator(builder);
                builder.Append(ForwardedHost);
                builder.Append(value.Host);
            }

            if (hasProto)
            {
                AppendSeperator(builder);
                builder.Append(ForwardedProto);
                builder.Append(value.Proto);
            }

            void AppendComma(StringBuilder sb)
            {
                if (!appendComma)
                    return;

                sb.Append(CommaAndSpace);
                appendComma = false;
            }

            void AppendSemicolon(StringBuilder sb)
            {
                if (appendComma)
                    return;

                sb.AppendIfNotEmpty(Semicolon);
            }

            void AppendSeperator(StringBuilder sb)
            {
                AppendSemicolon(sb);
                AppendComma(sb);
            }
        }

        /// <summary>
        /// This can be either:
        /// <list type="bullet">
        /// <item>an IP address(v4 or v6, optionally with a port, and ipv6 quoted and enclosed in square brackets),</item>
        /// <item>an obfuscated identifier (such as <c>"_hidden"</c> or <c>"_secret"</c>),</item>
        /// <item>or <c>"unknown"</c> when the preceding entity is not known (and we still want to indicate that forwarding of the request was made).</item>
        /// </list>
        /// </summary>
        private static string GetValidForwardedIdentifier(string identifier)
        {
            return IPAddress.TryParse(identifier, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6
                ? CreateValidForwardedIpAddressV6(ip)
                : identifier;
        }

        [return: NotNullIfNotNull("ip")]
        private static string? CreateValidForwardedIpAddress(IPAddress? ip)
        {
            if (ip is null)
                return null;

            return ip.AddressFamily == AddressFamily.InterNetworkV6
                ? CreateValidForwardedIpAddressV6(ip)
                : ip.ToString();
        }

        private static string CreateValidForwardedIpAddressV6(IPAddress ip)
            => $"\"[{ip}]\"";

        // internal for testing
        internal static IEnumerable<ForwardedHeaderValue> GetCurrentForwardedHeaderValues(IHeaderDictionary headers)
        {
            if (headers.TryGetValue(Names.XForwardedFor, out var forValues) && forValues.Count > 0)
            {
                bool isFirst = true;
                foreach (var @for in forValues)
                {
                    if (string.IsNullOrEmpty(@for))
                        continue;

                    if (!isFirst)
                    {
                        yield return new ForwardedHeaderValue(@for: @for);
                        continue;
                    }

                    isFirst = false;
                    headers.TryGetValue(Names.XForwardedHost, out var hostValues);
                    headers.TryGetValue(Names.XForwardedProto, out var protoValues);

                    yield return new ForwardedHeaderValue(
                        @for: @for,
                        host: hostValues.FirstOrDefault(),
                        proto: protoValues.FirstOrDefault());
                }
            }

            var forwardedValues = headers.GetCommaSeparatedValues(Names.Forwarded);
            if (forwardedValues.Length > 0)
            {
                foreach (var value in forwardedValues)
                {
                    if (TryParseForwardedHeaderValue(value, out var forwardedValue))
                        yield return forwardedValue;
                }
            }
        }

        private static bool TryParseForwardedHeaderValue(
            ReadOnlySpan<char> value,
            out ForwardedHeaderValue parsedValue)
        {
            var spans = value.Split(Semicolon);
            string? by = null;
            string? @for = null;
            string? host = null;
            string? proto = null;

            while (spans.MoveNext())
            {
                var current = spans.Current.Trim();
                if (current.StartsWith(ForwardedBy.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    by = current.Slice(ForwardedBy.Length).Trim().ToString();
                else if (current.StartsWith(ForwardedFor.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    @for = current.Slice(ForwardedFor.Length).Trim().ToString();
                else if (current.StartsWith(ForwardedHost.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    host = current.Slice(ForwardedHost.Length).Trim().ToString();
                else if (current.StartsWith(ForwardedProto.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    proto = current.Slice(ForwardedProto.Length).Trim().ToString();
            }

            if (string.IsNullOrEmpty(by) && string.IsNullOrEmpty(@for) && string.IsNullOrEmpty(host) && string.IsNullOrEmpty(proto))
            {
                parsedValue = default;
                return false;
            }

            parsedValue = new ForwardedHeaderValue(by, @for, host, proto);
            return true;
        }

        // internal for testing
        internal static bool TryParseIpV6Address(
            string value,
            [NotNullWhen(true)] out IPAddress? ip)
        {
            if (value.Length > 6 && value.StartsWithOrdinal("\"[") && value[^1] == '"')
            {
                var end = value.IndexOfOrdinal(']');
                value = value[2..end];
            }

            if (IPAddress.TryParse(value, out ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
                return true;

            ip = null;
            return false;
        }

        // internal for testing
        protected internal readonly struct ForwardedHeaderValue
        {
            internal ForwardedHeaderValue(
                string? by = null,
                string? @for = null,
                string? host = null,
                string? proto = null)
            {
                By = by;
                For = @for;
                Host = host;
                Proto = proto;
            }

            internal string? By { get; }

            internal string? For { get; }

            internal string? Host { get; }

            internal string? Proto { get; }
        }
    }
}
