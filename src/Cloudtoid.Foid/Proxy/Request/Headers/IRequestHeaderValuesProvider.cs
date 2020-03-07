namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    public interface IRequestHeaderValuesProvider
    {
        /// <summary>
        /// By default, headers with an empty value are dropped.
        /// </summary>
        bool AllowHeadersWithEmptyValue { get; }

        /// <summary>
        /// By default, headers with an underscore in their names are dropped.
        /// </summary>
        bool AllowHeadersWithUnderscoreInName { get; }

        /// <summary>
        /// If true, an "x-foid-external-address" header with the immediate downstream IP address is added to the outgoing upstream call.
        /// The default value is false.
        /// </summary>
        bool IncludeExternalAddress { get; }

        /// <summary>
        /// If false, it will copy all the headers from the incoming donstream request to the outgoing upstream request.
        /// The default value is false.
        /// </summary>
        bool IgnoreAllDownstreamRequestHeaders { get; }

        /// <summary>
        /// If false, it will append the IP address of the nearest client to the "x-forwarded-for" header.
        /// The default value is false.
        /// </summary>
        bool IgnoreClientAddress { get; }

        /// <summary>
        /// If false, it will append the client protocol (HTTP or HTTPS) to the "x-forwarded-proto" header.
        /// The default value is false.
        /// </summary>
        bool IgnoreClientProtocol { get; }

        /// <summary>
        /// If false, it will append a "x-request-id" header if not present.
        /// The default value is false.
        /// </summary>
        bool IgnoreRequestId { get; }

        /// <summary>
        /// If false, it will append a "x-call-id" header. This is a guid that is always new for each call.
        /// The default value is false.
        /// </summary>
        bool IgnoreCallId { get; }

        /// <summary>
        /// By implementing this method, one can change the values of a given header.
        /// Return false, if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(HttpContext context, string name, IList<string> downstreamValues, out IList<string> upstreamValues);

        /// <summary>
        /// If the incoming downstream request does not have a HOST header, the value provided here will be used.
        /// </summary>
        string GetDefaultHostHeaderValue(HttpContext context);

        /// <summary>
        /// If this is not null or empty, an "x-foid-proxy-name" header with this value is added to the outgoing upstream request.
        /// </summary>
        string? GetProxyNameHeaderValue(HttpContext context);

        /// <summary>
        /// Extra headers to be appended to the outgoing upstream request.
        /// </summary>
        IEnumerable<(string Key, string[] Values)> GetExtraHeaders(HttpContext context);
    }
}
