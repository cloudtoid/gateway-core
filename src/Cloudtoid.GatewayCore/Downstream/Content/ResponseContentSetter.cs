using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cloudtoid.GatewayCore.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Downstream
{
    /// <summary>
    /// By inheriting from this class, one can have full control over the outbound downstream response content and content header. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="ResponseContentHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseContentHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="ResponseContentSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseContentSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseContentHeaderValuesProvider"/>, MyResponseContentHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseContentSetter"/>, MyResponseContentSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public class ResponseContentSetter : IResponseContentSetter
    {
        private static readonly ISet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.TransferEncoding,
        };

        private readonly HeaderSanetizer sanetizer;

        public ResponseContentSetter(
            IResponseContentHeaderValuesProvider provider,
            ILogger<ResponseContentSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Logger = CheckValue(logger, nameof(logger));
            sanetizer = new HeaderSanetizer(logger);
        }

        protected IResponseContentHeaderValuesProvider Provider { get; }

        protected ILogger<ResponseContentSetter> Logger { get; }

        public virtual async Task SetContentAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            cancellationToken.ThrowIfCancellationRequested();

            if (upstreamResponse.Content is null)
            {
                Logger.LogDebug("The inbound upstream response does not have a content body.");
                return;
            }

            var contentLength = upstreamResponse.Content.Headers?.ContentLength;
            if (contentLength != null && contentLength <= 0)
            {
                Logger.LogDebug("The inbound upstream response has a content length that is less than or equal to zero.");
                return;
            }

            await SetContentHeadersAsync(context, upstreamResponse, cancellationToken);
            await SetContentBodyAsync(context, upstreamResponse, cancellationToken);
        }

        protected virtual async Task SetContentBodyAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            var downstreamResponseStream = context.Response.Body;

            using (var stream = await upstreamResponse.Content.ReadAsStreamAsync())
            {
                await stream.CopyToAsync(downstreamResponseStream, cancellationToken);
                await downstreamResponseStream.FlushAsync(cancellationToken);
            }
        }

        protected virtual Task SetContentHeadersAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            var options = context.ProxyDownstreamResponseHeaderSettings;
            if (options.DiscardInboundHeaders)
                return Task.CompletedTask;

            cancellationToken.ThrowIfCancellationRequested();

            var discardEmpty = options.DiscardEmpty;
            var discardUnderscore = options.DiscardUnderscore;

            foreach (var header in upstreamResponse.Content.Headers)
            {
                var name = header.Key;

                if (!sanetizer.IsValid(
                    name,
                    header.Value,
                    discardEmpty,
                    discardUnderscore))
                    continue;

                if (HeaderTransferBlacklist.Contains(name))
                    continue;

                AddHeaderValues(
                    context,
                    name,
                    header.Value.AsStringValues());
            }

            return Task.CompletedTask;
        }

        protected virtual void AddHeaderValues(
            ProxyContext context,
            string name,
            StringValues upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, upstreamValues, out var downstreamValues) && downstreamValues.Count > 0)
            {
                context.Response.Headers[name] = downstreamValues;
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by {1}.{2}.",
                name,
                nameof(IResponseContentHeaderValuesProvider),
                nameof(IResponseContentHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
