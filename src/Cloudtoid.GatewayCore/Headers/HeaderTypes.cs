using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Net.Http.Headers;

namespace Cloudtoid.GatewayCore.Headers
{
    // The list below came from System.Net.Http.Headers.KnownHeaders, System.Net.HttpRequestHeader and
    // System.Net.HttpResponseHeader. See .NET runtime on github.
    internal static class HeaderTypes
    {
        private static readonly IList<string> GeneralHeaders = new List<string>()
        {
            HeaderNames.CacheControl,
            HeaderNames.Connection,
            HeaderNames.Date,
            HeaderNames.KeepAlive,
            HeaderNames.Pragma,
            HeaderNames.Trailer,
            HeaderNames.TransferEncoding,
            HeaderNames.Upgrade,
            HeaderNames.Via,
            HeaderNames.Warning,
        };

        internal static readonly ISet<string> ContentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Allow,
            HeaderNames.ContentDisposition,
            HeaderNames.ContentEncoding,
            HeaderNames.ContentLanguage,
            HeaderNames.ContentLength,
            HeaderNames.ContentLocation,
            HeaderNames.ContentMD5,
            HeaderNames.ContentRange,
            HeaderNames.ContentType,
            HeaderNames.Expires,
            HeaderNames.LastModified,
        };

        internal static readonly ISet<string> RequestHeaders = new HashSet<string>(GeneralHeaders.Concat(ContentHeaders), StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Accept,
            HeaderNames.AcceptCharset,
            HeaderNames.AcceptEncoding,
            HeaderNames.AcceptLanguage,
            "Alt-Used",
            HeaderNames.Authorization,
            HeaderNames.Cookie,
            HeaderNames.Expect,
            HeaderNames.From,
            HeaderNames.Host,
            HeaderNames.IfMatch,
            HeaderNames.IfModifiedSince,
            HeaderNames.IfNoneMatch,
            HeaderNames.IfRange,
            HeaderNames.IfUnmodifiedSince,
            HeaderNames.MaxForwards,
            HeaderNames.ProxyAuthorization,
            HeaderNames.Range,
            HeaderNames.Referer,
            HeaderNames.TE,
            HeaderNames.Translate,
            HeaderNames.UserAgent,
        };

        internal static readonly ISet<string> ResponseHeaders = new HashSet<string>(GeneralHeaders.Concat(ContentHeaders), StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Status,
            HeaderNames.AcceptRanges,
            HeaderNames.Age,
            "Alt-Svc",
            HeaderNames.ETag,
            HeaderNames.Location,
            HeaderNames.ProxyAuthenticate,
            HeaderNames.RetryAfter,
            HeaderNames.Server,
            HeaderNames.SetCookie,
            HeaderNames.Vary,
            HeaderNames.WWWAuthenticate,
        };

        internal static readonly ISet<string> NonTrailingHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Age,
            HeaderNames.Authorization,
            HeaderNames.CacheControl,
            HeaderNames.ContentDisposition,
            HeaderNames.ContentEncoding,
            HeaderNames.ContentLength,
            HeaderNames.ContentLocation,
            HeaderNames.ContentRange,
            HeaderNames.ContentType,
            HeaderNames.Date,
            HeaderNames.Expect,
            HeaderNames.Expires,
            HeaderNames.Host,
            HeaderNames.IfMatch,
            HeaderNames.IfModifiedSince,
            HeaderNames.IfNoneMatch,
            HeaderNames.IfRange,
            HeaderNames.IfUnmodifiedSince,
            HeaderNames.Location,
            HeaderNames.MaxForwards,
            HeaderNames.Pragma,
            HeaderNames.ProxyAuthenticate,
            HeaderNames.ProxyAuthorization,
            HeaderNames.Range,
            HeaderNames.RetryAfter,
            HeaderNames.SetCookie,
            "Set-Cookie2",
            HeaderNames.TE,
            HeaderNames.Trailer,
            HeaderNames.TransferEncoding,
            HeaderNames.Vary,
            HeaderNames.Warning,
            HeaderNames.WWWAuthenticate,
        };

        internal static readonly ISet<string> StandardHopByHopeHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.KeepAlive,
            HeaderNames.TransferEncoding,
            HeaderNames.TE,
            HeaderNames.Connection,
            HeaderNames.Trailer,
            HeaderNames.Upgrade,
            HeaderNames.ProxyAuthenticate,
            HeaderNames.ProxyAuthorization,
        };

        internal static bool IsContentHeader(string headerName)
            => ContentHeaders.Contains(headerName);

        internal static bool IsRequestHeader(string headerName)
            => RequestHeaders.Contains(headerName);

        internal static bool IsResponseHeader(string headerName)
            => ResponseHeaders.Contains(headerName);

        internal static bool IsNonTrailingHeader(string headerName)
            => NonTrailingHeaders.Contains(headerName);

        /// <summary>
        /// These are the standard hop-by-hop headers that must be consumed by the proxy and not passed on.
        /// These headers are: Keep-Alive, Transfer-Encoding, TE, Connection, Trailer, Upgrade, Proxy-Authorization and Proxy-Authenticate
        /// Except for the standard hop-by-hop headers, any hop-by-hop headers used by the message must be listed in the Connection header,
        /// so that the first proxy knows it has to consume them and not forward them further. Standard hop-by-hop headers can be listed
        /// too (it is often the case of Keep-Alive, but this is not mandatory).
        /// See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Connection">here</a> for more information.
        /// </summary>
        internal static bool IsStandardHopByHopHeader(string headerName)
            => StandardHopByHopeHeaders.Contains(headerName);
    }
}