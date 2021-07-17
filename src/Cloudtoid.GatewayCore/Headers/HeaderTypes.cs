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
        /// <summary>
        /// These are the standard hop-by-hop headers that must be consumed by the proxy and not passed on.
        /// These headers are: Keep-Alive, Transfer-Encoding, TE, Connection, Trailer, Upgrade, Proxy-Authorization and Proxy-Authenticate
        /// Except for the standard hop-by-hop headers, any hop-by-hop headers used by the message must be listed in the Connection header,
        /// so that the first proxy knows it has to consume them and not forward them further. Standard hop-by-hop headers can be listed
        /// too (it is often the case of Keep-Alive, but this is not mandatory).
        /// See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Connection">here</a> for more information.
        /// </summary>
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

        internal static readonly IList<string> DoNotTransferRequestHeaders = new[]
        {
            // standard headers
            HeaderNames.Host,
            HeaderNames.Via,
            Names.Forwarded,
            Names.XForwardedFor,
            Names.XForwardedBy,
            Names.XForwardedHost,
            Names.XForwardedProto,

            // HTTP/2 pseudo headers (https://datatracker.ietf.org/doc/html/rfc7540#section-8.1.2.3)
            HeaderNames.Method,
            HeaderNames.Authority,
            HeaderNames.Scheme,
            HeaderNames.Path,

            // Gateway Core headers
            Names.ExternalAddress,
            Names.ProxyName
        }.ConcatOrEmpty(StandardHopByHopeHeaders).ToArray();

        internal static readonly IList<string> DoNotTransferResponseHeaders = new[]
        {
            // standard headers
            HeaderNames.Via,
            HeaderNames.Server,

            // HTTP/2 pseudo headers (https://datatracker.ietf.org/doc/html/rfc7540#section-8.1.2.4)
            HeaderNames.Status,

            // Gateway Core headers
            Names.ExternalAddress,
            Names.ProxyName,
        }.ConcatOrEmpty(StandardHopByHopeHeaders).ToArray();

        internal static bool IsStandardHopByHopHeader(string headerName)
            => StandardHopByHopeHeaders.Contains(headerName);
    }
}