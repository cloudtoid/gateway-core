namespace Cloudtoid.Foid.Downstream
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Headers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using static Contract;

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

            if (context.Response.ContentLength <= 0)
            {
                Logger.LogDebug("The inbound upstream response has a content length of zero.");
                return;
            }

            await SetContentHeadersAsync(context, upstreamResponse, cancellationToken);
            await SetContentBodyAsync(context, upstreamResponse, cancellationToken);
            await SetContentHeadersAsync(context, upstreamResponse, cancellationToken);
        }

        protected virtual async Task SetContentBodyAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            var downstreamResponseStream = context.Response.Body;

            // TODO: the current version of HttpContent.CopyToAsync doesn't expose the cancellation-token
            // However, they are working on fixing that. We should modify this code and pass in cancellationToken
            await upstreamResponse.Content.CopyToAsync(downstreamResponseStream);
            await downstreamResponseStream.FlushAsync(cancellationToken);
        }

        protected virtual Task SetContentHeadersAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            var options = context.ProxyDownstreamResponseHeaderSettings;
            if (options.IgnoreAllUpstreamHeaders)
                return Task.CompletedTask;

            cancellationToken.ThrowIfCancellationRequested();

            var headers = upstreamResponse.Content?.Headers;
            if (headers is null)
                return Task.CompletedTask;

            var allowHeadersWithEmptyValue = options.AllowHeadersWithEmptyValue;
            var allowHeadersWithUnderscoreInName = options.AllowHeadersWithUnderscoreInName;

            foreach (var header in headers)
            {
                var name = header.Key;

                if (!sanetizer.IsValid(
                    name,
                    header.Value,
                    allowHeadersWithEmptyValue,
                    allowHeadersWithUnderscoreInName))
                    continue;

                if (HeaderTransferBlacklist.Contains(name))
                    continue;

                AddHeaderValues(
                    context,
                    name,
                    header.Value.AsArray());
            }

            return Task.CompletedTask;
        }

        protected virtual void AddHeaderValues(
            ProxyContext context,
            string name,
            params string[] upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.Headers[name] = downstreamValues;
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by the {1}.{2}.",
                name,
                nameof(IResponseContentHeaderValuesProvider),
                nameof(IResponseContentHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
