namespace Cloudtoid.GatewayCore
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

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
                /// Gets or sets an expression that defines the URL of the upstream server to which the downstream request is forwarded to.
                /// This is a required property.
                /// </summary>
                public string? To { get; set; }

                /// <summary>
                /// Gets or sets an expression that defines the name of this proxy. This name is used in the following scenarios:
                /// <list type="bullet">
                /// <item>This value is used in the Via HTTP header send on the outbound upstream request, and also the outbound downstream response. The default value is <c>"gwcore"</c></item>
                /// <item>If this is not <see langword="null"/>, an <c>x-gwcore-proxy-name</c> header with this value is added to the outbound upstream request.</item>
                /// </list>
                /// </summary>
                public string? ProxyName { get; set; }

                /// <summary>
                /// Gets or sets the header name for passing the correlation identifier.
                /// The default value is <c>x-correlation-id</c>.
                /// </summary>
                public string? CorrelationIdHeader { get; set; }

                public UpstreamRequestOptions UpstreamRequest { get; set; } = new UpstreamRequestOptions();

                public DownstreamResponseOptions DownstreamResponse { get; set; } = new DownstreamResponseOptions();

                public sealed class UpstreamRequestOptions
                {
                    /// <summary>
                    /// This is the HTTP protocol for the outbound upstream request.
                    /// The default value if HTTP/2.0
                    /// </summary>
                    public string? HttpVersion { get; set; }

                    public HeadersOptions Headers { get; set; } = new HeadersOptions();

                    public SenderOptions Sender { get; set; } = new SenderOptions();

                    public sealed class HeadersOptions
                    {
                        /// <summary>
                        /// By default, headers with an empty value are dropped.
                        /// </summary>
                        public bool AllowHeadersWithEmptyValue { get; set; }

                        /// <summary>
                        /// By default, headers with an underscore in their names are dropped.
                        /// </summary>
                        public bool AllowHeadersWithUnderscoreInName { get; set; }

                        /// <summary>
                        /// If <c>true</c>, an <c>x-gwcore-external-address</c> header with the immediate downstream IP address is added to the outbound upstream call.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IncludeExternalAddress { get; set; }

                        /// <summary>
                        /// If <c>false</c>, it will copy all headers (expect for a few that are blocked) from the inbound downstream request to the outbound upstream request. This includes both request and content headers.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool DiscardInboundHeaders { get; set; }

                        /// <summary>
                        /// If <c>false</c>, it will append a <c>via</c> header. See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via">here</a> for more information.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IgnoreVia { get; set; }

                        /// <summary>
                        /// If <c>false</c>, it will append a correlation identifier header if not present. The actual header name is defined by <see cref="CorrelationIdHeader"/>
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IgnoreCorrelationId { get; set; }

                        /// <summary>
                        /// If <c>false</c>, it will append a <c>x-call-id</c> header. This is a guid that is always new for each call.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IgnoreCallId { get; set; }

                        /// <summary>
                        /// If <c>false</c>, it will set <c>forwarded</c> header or <c>x-forwarded-*</c> headers. Also see <see cref="UseXForwarded"/>.
                        /// The information included in this header(s) consist of:
                        /// <list type="bullet">
                        /// <item><term>By</term><description>The interface where the request came in to the proxy server.</description></item>
                        /// <item><term>For</term><description>The client that initiated the request and subsequent proxies in a chain of proxies.</description></item>
                        /// <item><term>Host</term><description>The Host request header field as received by the proxy.</description></item>
                        /// <item><term>Proto</term><description>Indicates which protocol was used to make the request (typically <c>HTTP</c> or <c>HTTPS</c>).</description></item>
                        /// </list>
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IgnoreForwarded { get; set; }

                        /// <summary>
                        /// If <c>false</c>, it will use <c>x-forwarded-*</c> headers instead of the standard <c>forwarded</c> header. Also see <see cref="IgnoreForwarded"/>.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool UseXForwarded { get; set; }

                        /// <summary>
                        /// Gets or sets headers to be appended to the outbound upstream requests, or
                        /// if a header already exists, its value is replaced with the new value specified here.
                        /// </summary>
                        public Dictionary<string, string[]> Overrides { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

                        /// <summary>
                        /// Gets or sets the inbound downstream headers that should be discarded.
                        /// </summary>
                        public HashSet<string> Discards { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

                    public sealed class SenderOptions
                    {
                        /// <summary>
                        /// TGets or sets the name of the <see cref="HttpClient"/> that will be used to send the request.
                        /// The default value is a system generated unique name.
                        /// </summary>
                        public string? HttpClientName { get; set; }

                        /// <summary>
                        /// Gets or sets the total timeout in milliseconds to wait for the outbound upstream request to complete.
                        /// </summary>
                        public string? TimeoutInMilliseconds { get; set; }

                        /// <summary>
                        /// Gets or sets the connect timeout in milliseconds.
                        /// By default, no timeout is set.
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
                        /// Gets or sets the maximum amount of data that can be drained from responses in bytes.
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
                    public HeadersOptions Headers { get; set; } = new HeadersOptions();

                    public sealed class HeadersOptions
                    {
                        /// <summary>
                        /// By default, headers with an empty value are dropped.
                        /// </summary>
                        public bool AllowHeadersWithEmptyValue { get; set; }

                        /// <summary>
                        /// By default, headers with an underscore in their names are dropped.
                        /// </summary>
                        public bool AllowHeadersWithUnderscoreInName { get; set; }

                        /// <summary>
                        /// If <c>false</c>, it will copy all headers from the inbound upstream response to the outbound downstream response. This includes response, content, and trailing headers.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool DiscardInboundHeaders { get; set; }

                        /// <summary>
                        /// If <c>false</c>, it will append a <c>via</c> header. See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via">here</a> for more information.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IgnoreVia { get; set; }

                        /// <summary>
                        /// If <c>true</c>, it will append a correlation identifier header to the outbound downstream response. The actual header name is defined by <see cref="CorrelationIdHeader"/>
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IncludeCorrelationId { get; set; }

                        /// <summary>
                        /// If <c>true</c>, it will append a <c>x-call-id</c> header. This is a guid that is always new for each call.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IncludeCallId { get; set; }

                        /// <summary>
                        /// If <c>true</c>, it will append a <c>server</c> header. See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Server">here</a> for more information.
                        /// The default value is <c>false</c>.
                        /// </summary>
                        public bool IncludeServer { get; set; }

                        /// <summary>
                        /// Gets or sets the list of cookie configurations that is applied to the
                        /// <c>set-cookie</c> headers in the inbound upstream response. If the cookie name is
                        /// symbol <c>'*'</c>, then the settings are applied to all <c>set-cookie</c> headers.
                        /// </summary>
                        public Dictionary<string, CookieOptions> Cookies { get; set; } = new Dictionary<string, CookieOptions>(StringComparer.OrdinalIgnoreCase);

                        /// <summary>
                        /// Gets or sets headers to be appended to the outbound downstream response, or
                        /// if a header already exists, its value is replaced with the new value specified here.
                        /// </summary>
                        public Dictionary<string, string[]> Overrides { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

                        /// <summary>
                        /// Gets or sets the inbound upstream headers that should be discarded.
                        /// </summary>
                        public HashSet<string> Discards { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                            /// The Domain attribute specifies those hosts to which the cookie will be sent.
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