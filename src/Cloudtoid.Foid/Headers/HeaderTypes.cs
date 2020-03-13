namespace Cloudtoid.Foid.Headers
{
    using System;
    using System.Collections.Generic;

    // The list below came from  System.Net.Http.Headers.KnownHeaders. See .net runtime on github.
    internal static class HeaderTypes
    {
        internal static readonly ISet<string> GeneralHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Microsoft.Net.Http.Headers.HeaderNames.CacheControl,
            Microsoft.Net.Http.Headers.HeaderNames.Connection,
            Microsoft.Net.Http.Headers.HeaderNames.Date,
            Microsoft.Net.Http.Headers.HeaderNames.Pragma,
            Microsoft.Net.Http.Headers.HeaderNames.Trailer,
            Microsoft.Net.Http.Headers.HeaderNames.TransferEncoding,
            Microsoft.Net.Http.Headers.HeaderNames.Upgrade,
            Microsoft.Net.Http.Headers.HeaderNames.Via,
            Microsoft.Net.Http.Headers.HeaderNames.Warning,
        };

        internal static readonly ISet<string> RequestHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Microsoft.Net.Http.Headers.HeaderNames.Accept,
            Microsoft.Net.Http.Headers.HeaderNames.AcceptCharset,
            Microsoft.Net.Http.Headers.HeaderNames.AcceptEncoding,
            Microsoft.Net.Http.Headers.HeaderNames.AcceptLanguage,
            "Alt-Used",
            Microsoft.Net.Http.Headers.HeaderNames.Authorization,
            Microsoft.Net.Http.Headers.HeaderNames.Expect,
            Microsoft.Net.Http.Headers.HeaderNames.From,
            Microsoft.Net.Http.Headers.HeaderNames.Host,
            Microsoft.Net.Http.Headers.HeaderNames.IfMatch,
            Microsoft.Net.Http.Headers.HeaderNames.IfModifiedSince,
            Microsoft.Net.Http.Headers.HeaderNames.IfNoneMatch,
            Microsoft.Net.Http.Headers.HeaderNames.IfRange,
            Microsoft.Net.Http.Headers.HeaderNames.IfUnmodifiedSince,
            Microsoft.Net.Http.Headers.HeaderNames.MaxForwards,
            Microsoft.Net.Http.Headers.HeaderNames.ProxyAuthorization,
            Microsoft.Net.Http.Headers.HeaderNames.Range,
            Microsoft.Net.Http.Headers.HeaderNames.Referer,
            Microsoft.Net.Http.Headers.HeaderNames.TE,
            Microsoft.Net.Http.Headers.HeaderNames.UserAgent,
        };

        internal static readonly ISet<string> ContentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Microsoft.Net.Http.Headers.HeaderNames.Allow,
            Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition,
            Microsoft.Net.Http.Headers.HeaderNames.ContentEncoding,
            Microsoft.Net.Http.Headers.HeaderNames.ContentLanguage,
            Microsoft.Net.Http.Headers.HeaderNames.ContentLength,
            Microsoft.Net.Http.Headers.HeaderNames.ContentLocation,
            Microsoft.Net.Http.Headers.HeaderNames.ContentMD5,
            Microsoft.Net.Http.Headers.HeaderNames.ContentRange,
            Microsoft.Net.Http.Headers.HeaderNames.ContentType,
            Microsoft.Net.Http.Headers.HeaderNames.Expires,
            Microsoft.Net.Http.Headers.HeaderNames.LastModified,
        };

        internal static readonly ISet<string> ResponseHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Microsoft.Net.Http.Headers.HeaderNames.Status,
            Microsoft.Net.Http.Headers.HeaderNames.AcceptRanges,
            Microsoft.Net.Http.Headers.HeaderNames.Age,
            "Alt-Svc",
            Microsoft.Net.Http.Headers.HeaderNames.ETag,
            Microsoft.Net.Http.Headers.HeaderNames.Location,
            Microsoft.Net.Http.Headers.HeaderNames.ProxyAuthenticate,
            Microsoft.Net.Http.Headers.HeaderNames.RetryAfter,
            Microsoft.Net.Http.Headers.HeaderNames.Server,
            Microsoft.Net.Http.Headers.HeaderNames.Vary,
            Microsoft.Net.Http.Headers.HeaderNames.WWWAuthenticate,
        };

        internal static readonly ISet<string> NonTrailingHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Microsoft.Net.Http.Headers.HeaderNames.Age,
            Microsoft.Net.Http.Headers.HeaderNames.Authorization,
            Microsoft.Net.Http.Headers.HeaderNames.CacheControl,
            Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition,
            Microsoft.Net.Http.Headers.HeaderNames.ContentEncoding,
            Microsoft.Net.Http.Headers.HeaderNames.ContentLength,
            Microsoft.Net.Http.Headers.HeaderNames.ContentLocation,
            Microsoft.Net.Http.Headers.HeaderNames.ContentRange,
            Microsoft.Net.Http.Headers.HeaderNames.ContentType,
            Microsoft.Net.Http.Headers.HeaderNames.Date,
            Microsoft.Net.Http.Headers.HeaderNames.Expect,
            Microsoft.Net.Http.Headers.HeaderNames.Expires,
            Microsoft.Net.Http.Headers.HeaderNames.Host,
            Microsoft.Net.Http.Headers.HeaderNames.IfMatch,
            Microsoft.Net.Http.Headers.HeaderNames.IfModifiedSince,
            Microsoft.Net.Http.Headers.HeaderNames.IfNoneMatch,
            Microsoft.Net.Http.Headers.HeaderNames.IfRange,
            Microsoft.Net.Http.Headers.HeaderNames.IfUnmodifiedSince,
            Microsoft.Net.Http.Headers.HeaderNames.Location,
            Microsoft.Net.Http.Headers.HeaderNames.MaxForwards,
            Microsoft.Net.Http.Headers.HeaderNames.Pragma,
            Microsoft.Net.Http.Headers.HeaderNames.ProxyAuthenticate,
            Microsoft.Net.Http.Headers.HeaderNames.ProxyAuthorization,
            Microsoft.Net.Http.Headers.HeaderNames.Range,
            Microsoft.Net.Http.Headers.HeaderNames.RetryAfter,
            Microsoft.Net.Http.Headers.HeaderNames.SetCookie,
            "Set-Cookie2",
            Microsoft.Net.Http.Headers.HeaderNames.TE,
            Microsoft.Net.Http.Headers.HeaderNames.Trailer,
            Microsoft.Net.Http.Headers.HeaderNames.TransferEncoding,
            Microsoft.Net.Http.Headers.HeaderNames.Vary,
            Microsoft.Net.Http.Headers.HeaderNames.Warning,
            Microsoft.Net.Http.Headers.HeaderNames.WWWAuthenticate,
        };

        internal static bool IsContentHeader(string headerName)
                => ContentHeaders.Contains(headerName);
    }
}