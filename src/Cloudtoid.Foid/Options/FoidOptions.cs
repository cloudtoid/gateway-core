namespace Cloudtoid.Foid
{
    using System;

    public partial class FoidOptions
    {
        public ProxyOptions Proxy { get; set; } = new ProxyOptions();

        public partial class ProxyOptions
        {
            public UpstreamOptions Upstream { get; set; } = new UpstreamOptions();

            public sealed partial class UpstreamOptions
            {
                public RequestOptions Request { get; set; } = new RequestOptions();

                public sealed partial class RequestOptions
                {
                    /// <summary>
                    /// This is the total timeout in milliseconds to wait for the outbound upstream proxy call to complete
                    /// </summary>
                    public long TimeoutInMilliseconds { get; set; } = 240000;

                    public HeadersOptions Headers { get; set; } = new HeadersOptions();

                    public sealed partial class HeadersOptions
                    {
                        /// <summary>
                        /// By default, headers with an empty value are dropped.
                        /// </summary>
                        public bool AllowHeadersWithEmptyValue { get; set; } = false;

                        /// <summary>
                        /// By default, headers with an underscore in their names are dropped.
                        /// </summary>
                        public bool AllowHeadersWithUnderscoreInName { get; set; } = false;

                        /// <summary>
                        /// If true, an "x-foid-external-address" header with the immediate downstream IP address is added to the outgoing upstream call.
                        /// The default value is false.
                        /// </summary>
                        public bool IncludeExternalAddress { get; set; } = false;

                        /// <summary>
                        /// If false, it will append the IP address of the nearest client to the "x-forwarded-for" header.
                        /// The default value is false.
                        /// </summary>
                        public bool IgnoreClientAddress { get; set; } = false;

                        /// <summary>
                        /// If false, it will append the client protocol (HTTP or HTTPS) to the "x-forwarded-proto" header.
                        /// The default value is false.
                        /// </summary>
                        public bool IgnoreClientProtocol { get; set; } = false;

                        /// <summary>
                        /// If false, it will append a "x-request-id" header if not present.
                        /// The default value is false.
                        /// </summary>
                        public bool IgnoreRequestId { get; set; } = false;

                        /// <summary>
                        /// If false, it will append a "x-call-id" header. This is a guid that is always new for each call.
                        /// The default value is false.
                        /// </summary>
                        public bool IgnoreCallId { get; set; } = false;

                        /// <summary>
                        /// If the incoming downstream request does not have a HOST header, the value provided here will be used.
                        /// </summary>
                        public string DefaultHost { get; set; } = Environment.MachineName;

                        /// <summary>
                        /// If this is not null, an "x-foid-proxy-name" header with this value is added to the outgoing upstream call.
                        /// </summary>
                        public string? ProxyName { get; set; } = "foid";

                        /// <summary>
                        /// Extra headers to be appended to the outgoing upstream request
                        /// </summary>
                        public ExtraHeader[] ExtraHeaders { get; set; } = Array.Empty<ExtraHeader>();

                        public class ExtraHeader
                        {
                            public string Key { get; set; } = string.Empty;

                            public string[] Values { get; set; } = Array.Empty<string>();
                        }
                    }
                }
            }
        }
    }
}