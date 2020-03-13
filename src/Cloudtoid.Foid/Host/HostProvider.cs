namespace Cloudtoid.Foid.Host
{
    using Cloudtoid.Foid.Options;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    /// <summary>
    /// By inheriting from this class, one can override the HOST header of the outbound upstream request.
    /// You can also implement <see cref="IHostProvider"/> and register it with DI.
    /// </summary>
    public class HostProvider : IHostProvider
    {
        private static readonly object HostKey = new object();
        private readonly OptionsProvider options;

        public HostProvider(OptionsProvider options)
        {
            this.options = CheckValue(options, nameof(options));
        }

        public virtual string GetHost(HttpContext context)
        {
            // look up in out request cache first
            if (context.Items.TryGetValue(HostKey, out var existingHost))
                return (string)existingHost;

            var headersOptions = options.Proxy.Upstream.Request.Headers;
            if (headersOptions.IgnoreAllDownstreamHeaders)
                return CreateHost(context);

            if (!context.Request.Headers.TryGetValue(HeaderNames.Host, out var values) || values.Count == 0)
                return CreateHost(context);

            // If the HOST header includes a PORT number, remove the port number
            var host = GetHostWithoutPortNumber(values[0]);

            context.Items.Add(HostKey, host);
            return host;
        }

        private string CreateHost(HttpContext context)
        {
            var host = options.Proxy.Upstream.Request.Headers.GetDefaultHost(context);
            context.Items.Add(HostKey, host); // cache the value
            return host;
        }

        private static string GetHostWithoutPortNumber(string host)
        {
            var portIndex = host.LastIndexOf(':');
            return portIndex == -1 ? host : host.Substring(0, portIndex);
        }
    }
}
