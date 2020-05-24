namespace Cloudtoid.GatewayCore.Upstream
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore.Headers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using static Contract;

    /// <summary>
    /// By inheriting from this class, one can have full control over the outbound upstream content and its content headers. Please consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="RequestContentHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestContentHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="RequestContentSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestContentSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestContentHeaderValuesProvider"/>, MyRequestContentHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestContentSetter"/>, MyRequestContentSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public class RequestContentSetter : IRequestContentSetter
    {
        public RequestContentSetter(
            IRequestContentHeaderValuesProvider provider,
            ILogger<RequestContentSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Logger = CheckValue(logger, nameof(logger));
        }

        protected IRequestContentHeaderValuesProvider Provider { get; }

        protected ILogger<RequestContentSetter> Logger { get; }

        /// <inheritdoc/>
        public virtual async Task SetContentAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamRequest, nameof(upstreamRequest));

            cancellationToken.ThrowIfCancellationRequested();

            var body = context.Request.Body;
            if (body is null)
            {
                Logger.LogDebug("The inbound downstream request does not have a content body.");
                return;
            }

            if (!context.Request.ContentLength.HasValue)
            {
                Logger.LogDebug("The inbound downstream request does not specify a 'Content-Length'.");
                return;
            }

            await SetContentBodyAsync(context, upstreamRequest, cancellationToken);
            await SetContentHeadersAsync(context, upstreamRequest, cancellationToken);
        }

        protected virtual Task SetContentBodyAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            var body = context.Request.Body;
            if (!body.CanRead)
            {
                Logger.LogError("The inbound downstream request does not have a readable request body.");
                throw new InvalidOperationException("The inbound downstream request does not have a readable request body.");
            }

            if (body.CanSeek && body.Position != 0)
            {
                Logger.LogDebug("The inbound downstream request has a seek-able body stream. Resetting the stream to the beginning.");
                body.Position = 0;
            }

            upstreamRequest.Content = new StreamContent(context.Request.Body);
            return Task.CompletedTask;
        }

        protected virtual Task SetContentHeadersAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            if (context.ProxyUpstreamRequestHeadersSettings.DiscardInboundHeaders)
                return Task.CompletedTask;

            foreach (var header in context.Request.Headers)
            {
                var name = header.Key;

                if (!HeaderTypes.IsContentHeader(name))
                    continue;

                AddHeaderValues(
                    context,
                    upstreamRequest,
                    name,
                    header.Value);
            }

            return Task.CompletedTask;
        }

        protected virtual void AddHeaderValues(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            string name,
            StringValues downstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, downstreamValues, out var upstreamValues) && upstreamValues.Count > 0)
            {
                upstreamRequest.Content.Headers.TryAddWithoutValidation(name, (IEnumerable<string>)upstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by {1}.{2}.",
                name,
                nameof(IRequestContentHeaderValuesProvider),
                nameof(IRequestContentHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
