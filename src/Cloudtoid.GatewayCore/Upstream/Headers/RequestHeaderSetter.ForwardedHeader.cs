namespace Cloudtoid.GatewayCore.Upstream
{
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

    public partial class RequestHeaderSetter
    {
        private const string ForwardedBy = "by=";
        private const string ForwardedFor = "for=";
        private const string ForwardedProto = "proto=";
        private const string ForwardedHost = "host=";
        private const string CommaAndSpace = ", ";
        private const char Semicolon = ';';
        private const char Comma = ',';

        protected virtual void AddForwardedHeaders(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersSettings.IgnoreForwarded)
                return;

            if (context.ProxyUpstreamRequestHeadersSettings.UseXForwarded)
            {
                AddXForwardedHeaders(context, upstreamRequest);
                return;
            }

            AddForwardedHeader(context, upstreamRequest);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded
        protected virtual void AddForwardedHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var latestValue = CreateLatestForwardHeaderValue(context);

            var value = context.ProxyUpstreamRequestHeadersSettings.IgnoreAllDownstreamHeaders
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
            AddXForwardedForHeader(context, upstreamRequest);
            AddXForwardedProtocolHeader(context, upstreamRequest);
            AddXForwardedHostHeader(context, upstreamRequest);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-For
        protected virtual void AddXForwardedForHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var forAddress = GetRemoteIpAddressOrDefault(context);
            if (string.IsNullOrEmpty(forAddress))
                return;

            if (!context.ProxyUpstreamRequestHeadersSettings.IgnoreAllDownstreamHeaders)
            {
                StringBuilder? builder = null;

                foreach (var value in GetCurrentForwardedHeaderValues(context.Request.Headers))
                {
                    var @for = value.For;
                    if (!string.IsNullOrEmpty(@for))
                    {
                        if (builder is null)
                            builder = new StringBuilder(@for);
                        else
                            builder.Append(CommaAndSpace).Append(@for);
                    }
                }

                if (builder != null)
                    forAddress = builder.Append(CommaAndSpace).Append(forAddress).ToString();
            }

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.XForwardedFor,
                forAddress);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Proto
        protected virtual void AddXForwardedProtocolHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            if (string.IsNullOrEmpty(context.Request.Scheme))
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.XForwardedProto,
                context.Request.Scheme);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Host
        protected virtual void AddXForwardedHostHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var host = context.Request.Host;
            if (!host.HasValue)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.XForwardedHost,
                host.Value);
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
                builder.Append(value.By);
            }

            if (hasFor)
            {
                AppendSeperator(builder);
                builder.Append(ForwardedFor);
                builder.Append(value.For);
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

        [return: NotNullIfNotNull("ip")]
        private static string? CreateValidForwardedIpAddress(IPAddress? ip)
        {
            if (ip is null)
                return null;

            return ip.AddressFamily == AddressFamily.InterNetworkV6
                ? $"\"[{ip}]\""
                : ip.ToString();
        }

        // internal for testing
        internal static IEnumerable<ForwardedHeaderValue> GetCurrentForwardedHeaderValues(IHeaderDictionary headers)
        {
            if (headers.TryGetValue(Names.XForwardedFor, out var forValues) && forValues.Count > 0)
            {
                headers.TryGetValue(Names.XForwardedHost, out var hostValues);
                headers.TryGetValue(Names.XForwardedProto, out var protoValues);

                string? host = hostValues.FirstOrDefault();
                string? proto = protoValues.FirstOrDefault();
                foreach (var @for in forValues)
                {
                    if (!string.IsNullOrEmpty(@for))
                        yield return new ForwardedHeaderValue(@for: @for, host: host, proto: proto);
                }
            }

            if (headers.TryGetValue(Names.Forwarded, out var forwardedValues) && forwardedValues.Count > 0)
            {
                if (TryParseForwardedHeaderValues(forwardedValues[0], out var values))
                {
                    foreach (var value in values)
                        yield return value;
                }
            }
        }

        // internal for testing
        internal static bool TryParseForwardedHeaderValues(
            string value,
            [NotNullWhen(true)] out IReadOnlyList<ForwardedHeaderValue>? parsedValues)
        {
            List<ForwardedHeaderValue>? list = null;
            var spans = value.AsSpan().Split(Comma);
            while (spans.MoveNext())
            {
                if (TryParseForwardedHeaderValue(spans.Current.Trim(), out var headerValue))
                {
                    if (list is null)
                        list = new List<ForwardedHeaderValue>(4);

                    list.Add(headerValue);
                }
            }

            parsedValues = list;
            return parsedValues != null;
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
        internal struct ForwardedHeaderValue
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
