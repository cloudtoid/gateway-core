namespace Cloudtoid.Foid
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    internal enum HttpProtocolVersion
    {
        Http10 = 10,
        Http11 = 11,
        Http20 = 20,
        Http30 = 30,
        Http40 = 40,
    }

    internal static class HttpRequestExtensions
    {
        private const HttpProtocolVersion DefaultHttpVersion = HttpProtocolVersion.Http11;
        private static readonly IReadOnlyDictionary<string, HttpProtocolVersion> HttpVersionMap = new Dictionary<string, HttpProtocolVersion>(StringComparer.OrdinalIgnoreCase)
        {
            { "HTTP/1", HttpProtocolVersion.Http10 },
            { "HTTP/1.0", HttpProtocolVersion.Http10 },
            { "HTTP/1.1", HttpProtocolVersion.Http11 },
            { "HTTP/2", HttpProtocolVersion.Http20 },
            { "HTTP/2.0", HttpProtocolVersion.Http20 },
            { "HTTP/3", HttpProtocolVersion.Http30 },
            { "HTTP/3.0", HttpProtocolVersion.Http30 },
            { "HTTP/4", HttpProtocolVersion.Http40 },
            { "HTTP/4.0", HttpProtocolVersion.Http40 },
        };

        internal static HttpProtocolVersion GetHttpVersion(this HttpRequest request)
        {
            var protocol = request.Protocol;
            if (string.IsNullOrEmpty(protocol))
                return DefaultHttpVersion;

            if (HttpVersionMap.TryGetValue(protocol, out var version))
                return version;

            if (protocol.ContainsOrdinalIgnoreCase("/1.1"))
                return HttpProtocolVersion.Http11;

            if (protocol.ContainsOrdinalIgnoreCase("/1"))
                return HttpProtocolVersion.Http10;

            if (protocol.ContainsOrdinalIgnoreCase("/2"))
                return HttpProtocolVersion.Http20;

            if (protocol.ContainsOrdinalIgnoreCase("/3"))
                return HttpProtocolVersion.Http30;

            if (protocol.ContainsOrdinalIgnoreCase("/4"))
                return HttpProtocolVersion.Http40;

            return DefaultHttpVersion;
        }
    }
}
