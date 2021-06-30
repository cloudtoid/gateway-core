#pragma warning disable CA1034 // Nested types should not be visible

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Cloudtoid.GatewayCore
{
    public sealed class GatewayOptions
    {
        /// <summary>
        /// Gets or sets the system-wide configurations.
        /// </summary>
        public SystemOptions System { get; set; } = new SystemOptions();

        /// <summary>
        /// Gets or sets the list of proxy server route configurations
        /// The key here is the relative URL of the inbound downstream request.
        /// </summary>
        public Dictionary<string, RouteOptions> Routes { get; set; } = new Dictionary<string, RouteOptions>(StringComparer.OrdinalIgnoreCase);

        public sealed class SystemOptions
        {
            /// <summary>
            /// Gets or sets the maximum number of mappings between "inbound downstream request path" and
            /// "outbound upstream request URL" that can be cached in memory.
            /// The default value is 100,000 entries.
            /// </summary>
            public int? RouteCacheMaxCount { get; set; }
        }

        public sealed class RouteOptions
        {
            /// <summary>
            /// Gets or sets the proxy configuration depending on a relative request URL specified by <see cref="Route"/>.
            /// </summary>
            public ProxyOptions? Proxy { get; set; }

            public sealed class ProxyOptions
            {
                /// <summary>
                /// Gets or sets the URL of the upstream server to which the downstream request is forwarded to.
                /// This is a required property and can be an expression.
                /// </summary>
                public string? To { get; set; }

                /// <summary>
                /// Gets or sets the name of this proxy. This name is used in the following scenarios and can be an expression:
                /// <list type="bullet">
                /// <item>This value is used in the Via HTTP header send on the outbound upstream request, and also the outbound downstream response. The default value is <c>"gwcore"</c></item>
                /// <item>If this is not <see langword="null"/>, an <c>x-gwcore-proxy-name</c> header with this value is added to the outbound upstream request.</item>
                /// </list>
                /// </summary>
                public string? ProxyName { get; set; }

                /// <summary>
                /// Gets or sets the name of headers that hold correlation identifiers.
                /// The default value is <c>x-correlation-id</c> and it can be an expression.
                /// </summary>
                public string? CorrelationIdHeader { get; set; }

                /// <summary>
                /// Gets or sets the options that control the upstream requests.
                /// </summary>
                public UpstreamRequestOptions UpstreamRequest { get; set; } = new UpstreamRequestOptions();

                /// <summary>
                /// Gets or sets the options that control the downstream responses sent to the clients.
                /// </summary>
                public DownstreamResponseOptions DownstreamResponse { get; set; } = new DownstreamResponseOptions();

                public sealed class UpstreamRequestOptions
                {
                    /// <summary>
                    /// Gets or sets an expression that defines the HTTP protocol of outbound upstream requests.
                    /// The default value if HTTP/2.0.
                    /// </summary>
                    public string? HttpVersion { get; set; }

                    /// <summary>
                    /// Gets or sets the options that control the upstream request headers.
                    /// </summary>
                    public HeadersOptions Headers { get; set; } = new HeadersOptions();

                    /// <summary>
                    /// Gets or sets the options that control connection to the upstream server.
                    /// </summary>
                    public SenderOptions Sender { get; set; } = new SenderOptions();

                    public sealed class HeadersOptions
                    {
                        /// <summary>
                        /// Gets or sets if downstream inbound request and content headers should be discarded and not forwarded to the upstream.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool DiscardInboundHeaders { get; set; }

                        /// <summary>
                        /// Gets or sets if inbound headers with empty value should be discarded.
                        /// The default value is <c>false</c>, meaning that headers with empty value are kept.
                        /// </summary>
                        public bool DiscardEmpty { get; set; }

                        /// <summary>
                        /// Gets or sets if inbound headers with an underscore in their name should be discarded.
                        /// The default value is <c>false</c>, meaning that headers with an underscore in their name are kept.
                        /// </summary>
                        public bool DiscardUnderscore { get; set; }

                        /// <summary>
                        /// Gets or sets the inbound downstream headers that should be discarded.
                        /// </summary>
                        public HashSet<string> Discards { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        /// <summary>
                        /// Gets or sets if a <c>x-gwcore-external-address</c> header with the IP address of the immediate caller should be added to the outbound upstream call.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool AddExternalAddress { get; set; }

                        /// <summary>
                        /// Gets or sets if a `x-correlation-id` header should be skipped from the outbound upstream request if not already present.
                        /// The default value is <c>false</c>, meaning that a correlation identifier header is included.
                        /// The name of this header is <c>x-correlation-id</c>, but it can be altered using <see cref="CorrelationIdHeader"/>.
                        /// </summary>
                        public bool SkipCorrelationId { get; set; }

                        /// <summary>
                        /// Gets or sets if an <c>x-call-id</c> header should be skipped. This is a <c>guid</c> that is generated on each call.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool SkipCallId { get; set; }

                        /// <summary>
                        /// Gets or sets if a <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via"><c>via</c></a> header should be skipped from the outbound request.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool SkipVia { get; set; }

                        /// <summary>
                        /// Gets or sets if <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded"><c>forwarded</c></a>
                        /// or <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-For"><c>x-forwarded-*</c></a> headers
                        /// should be skipped from the outbound requests. The default value is <c>false</c> and the final header is decided based on the value of
                        /// <see cref="UseXForwarded"/>.
                        /// The information captured by these headers consist of:
                        /// <list type="bullet">
                        /// <item><term>By</term><description>The interface where the request came in to the proxy server.</description></item>
                        /// <item><term>For</term><description>The client that initiated the request and subsequent proxies in a chain of proxies.</description></item>
                        /// <item><term>Host</term><description>The Host request header field as received by the proxy.</description></item>
                        /// <item><term>Proto</term><description>Indicates which protocol was used to make the request (typically <c>HTTP</c> or <c>HTTPS</c>).</description></item>
                        /// </list>
                        /// </summary>
                        public bool SkipForwarded { get; set; }

                        /// <summary>
                        /// Gets or sets if <c>x-forwarded-*</c> headers should be used instead of the standard
                        /// <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded"><c>forwarded</c></a> header.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool UseXForwarded { get; set; }

                        /// <summary>
                        /// Gets or sets headers to be appended to the outbound upstream requests, or
                        /// if the header already exists, its value is replaced with the new value specified here.
                        /// The value can be either text or an expression.
                        /// </summary>
                        public Dictionary<string, string[]> Overrides { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                    }

                    public sealed class SenderOptions
                    {
                        /// <summary>
                        /// Gets or sets the name of the <see cref="HttpClient"/> that will be used to send the request.
                        /// The default value is a system generated unique name.
                        /// </summary>
                        public string? HttpClientName { get; set; }

                        /// <summary>
                        /// Gets or sets the total timeout in milliseconds to wait for the outbound upstream request to complete. This can be an expression.
                        /// </summary>
                        public string? TimeoutInMilliseconds { get; set; }

                        /// <summary>
                        /// Gets or sets the connect timeout in milliseconds.
                        /// No timeout is set by default.
                        /// </summary>
                        public int? ConnectTimeoutInMilliseconds { get; set; }

                        /// <summary>
                        /// Gets or sets the expect 100 continue timeout in milliseconds.
                        /// The default value is 1 second.
                        /// </summary>
                        public int? Expect100ContinueTimeoutInMilliseconds { get; set; }

                        /// <summary>
                        /// Gets or sets how long, in milliseconds, a connection can be idle in the pool to be considered reusable. Also see <see cref="PooledConnectionLifetimeInMilliseconds"/>.
                        /// The default value is 2 minutes.
                        /// </summary>
                        public int? PooledConnectionIdleTimeoutInMilliseconds { get; set; }

                        /// <summary>
                        /// Gets or sets how long, in milliseconds, a connection can live in the connection pool.
                        /// By default, no timeout is set and the connection can stay in the pool. Also see <see cref="PooledConnectionIdleTimeoutInMilliseconds"/>.
                        /// </summary>
                        public int? PooledConnectionLifetimeInMilliseconds { get; set; }

                        /// <summary>
                        /// Gets or sets the response drain timeout.
                        /// The default value is 2 seconds.
                        /// </summary>
                        public int? ResponseDrainTimeoutInMilliseconds { get; set; }

                        /// <summary>
                        /// Gets or sets the maximum number of allowed HTTP redirects.
                        /// The default value is 50.
                        /// </summary>
                        public int? MaxAutomaticRedirections { get; set; }

                        /// <summary>
                        /// Gets or sets the maximum number of simultaneous TCP connections allowed to a single server.
                        /// The default value is <see cref="int.MaxValue"/>.
                        /// </summary>
                        public int? MaxConnectionsPerServer { get; set; }

                        /// <summary>
                        /// Gets or sets the maximum size of data that can be drained from responses in bytes.
                        /// The default value is <c>1024 * 1024</c>.
                        /// </summary>
                        public int? MaxResponseDrainSizeInBytes { get; set; }

                        /// <summary>
                        /// Gets or sets the maximum length, in kilobytes (1024 bytes), of the response headers.
                        /// The default value is 64 kilobytes.
                        /// </summary>
                        public int? MaxResponseHeadersLengthInKilobytes { get; set; }

                        /// <summary>
                        /// Gets or sets a value that indicates whether the HTTP handler used by the outbound upstream request sender (<see cref="Upstream.IRequestSender"/>)
                        /// should follow redirection responses.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool AllowAutoRedirect { get; set; }

                        /// <summary>
                        /// Gets or sets a value that indicates whether the HTTP handler used by the outbound upstream request sender (<see cref="Upstream.IRequestSender"/>)
                        /// uses the <see cref="HttpClientHandler.CookieContainer"/> property to store server cookies and uses these cookies when sending requests.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool UseCookies { get; set; }
                    }
                }

                public sealed class DownstreamResponseOptions
                {
                    /// <summary>
                    /// Gets or sets the options that control the downstream response headers sent to the clients.
                    /// </summary>
                    public HeadersOptions Headers { get; set; } = new HeadersOptions();

                    public sealed class HeadersOptions
                    {
                        /// <summary>
                        /// Gets or sets if upstream inbound response, content, and trailing headers should be discarded and not forwarded to the downstream client.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool DiscardInboundHeaders { get; set; }

                        /// <summary>
                        /// Gets or sets if inbound headers with empty value should be discarded.
                        /// The default value is <c>false</c>, meaning that headers with empty value are kept.
                        /// </summary>
                        public bool DiscardEmpty { get; set; }

                        /// <summary>
                        /// Gets or sets if inbound headers with an underscore in their name should be discarded.
                        /// The default value is <c>false</c>, meaning that headers with an underscore in their name are kept.
                        /// </summary>
                        public bool DiscardUnderscore { get; set; }

                        /// <summary>
                        /// Gets or sets the inbound upstream headers that should be discarded.
                        /// </summary>
                        public HashSet<string> Discards { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        /// <summary>
                        /// Gets or sets if a <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Server"><c>server</c></a> header should be added. The value of this header is set to <c>gwcore</c>.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool AddServer { get; set; }

                        /// <summary>
                        /// Gets or sets if a correlation identifier header, if not present on the inbound response, should be added
                        /// to the outbound response. The default value is <c>false</c>, meaning that a correlation identifier header
                        /// is not included.
                        /// The name of this header is <c>x-correlation-id</c>, but it can be altered using <see cref="CorrelationIdHeader"/>.
                        /// </summary>
                        public bool AddCorrelationId { get; set; }

                        /// <summary>
                        /// Gets or sets if a <c>x-call-id</c> header should be added. This is a <c>guid</c> that is generated on each call.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool AddCallId { get; set; }

                        /// <summary>
                        /// Gets or sets if the <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via"><c>via</c></a> header should be skipped from the outbound response.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool SkipVia { get; set; }

                        /// <summary>
                        /// Gets or sets the options that are applied to the <c>set-cookie</c> headers in the inbound upstream response.
                        /// If the cookie name is symbol <c>'*'</c>, then the settings are applied to all <c>set-cookie</c> headers.
                        /// </summary>
                        public Dictionary<string, CookieOptions> Cookies { get; set; } = new Dictionary<string, CookieOptions>(StringComparer.OrdinalIgnoreCase);

                        /// <summary>
                        /// Gets or sets headers to be appended to the outbound downstream response, or
                        /// if the header already exists, its value is replaced with the new value specified here.
                        /// </summary>
                        public Dictionary<string, string[]> Overrides { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

                        public sealed class CookieOptions
                        {
                            /// <summary>
                            /// The Secure attribute limits the scope of the cookie to "secure" channels.
                            /// A secure cookie is only sent to the server when a request is made using https.
                            /// </summary>
                            public bool? Secure { get; set; }

                            /// <summary>
                            /// The HttpOnly attribute limits the scope of the cookie to HTTP
                            /// requests, forbidding JavaScript from accessing the cookie.
                            /// </summary>
                            public bool? HttpOnly { get; set; }

                            /// <summary>
                            /// The SameSite attribute asserts that a cookie must not be sent with cross-origin requests,
                            /// providing some protection against cross-site request forgery attacks
                            /// The valid values are <c>strict</c>, <c>lax</c>, and <c>none</c>.
                            /// </summary>
                            public string? SameSite { get; set; }

                            /// <summary>
                            /// The Domain attribute specifies the hosts to which the cookie will be sent and can be an expression.
                            /// For example, if the value of the Domain attribute is "example.com", the user
                            /// agent will include the cookie in the Cookie header when making HTTP requests
                            /// to example.com, www.example.com, and www.corp.example.com.
                            /// Use this property to specify or override the domain attribute.
                            /// Set this value to an empty string (<c>""</c>) to remove the domain attribute from the cookie.
                            /// </summary>
                            public string? Domain { get; set; }
                        }
                    }
                }
            }
        }
    }
}