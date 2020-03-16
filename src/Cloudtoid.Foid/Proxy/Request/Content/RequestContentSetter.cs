namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Headers;
    using Cloudtoid.Foid.Options;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
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
            OptionsProvider options,
            ILogger<RequestContentSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Options = CheckValue(options, nameof(options));
            Logger = CheckValue(logger, nameof(logger));
        }

        protected IRequestContentHeaderValuesProvider Provider { get; }

        protected ILogger<RequestContentSetter> Logger { get; }

        protected OptionsProvider Options { get; }

        /// <inheritdoc/>
        public virtual async Task SetContentAsync(
            HttpContext context,
            HttpRequestMessage upstreamRequest)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamRequest, nameof(upstreamRequest));

            context.RequestAborted.ThrowIfCancellationRequested();

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

            await SetContentBodyAsync(context, upstreamRequest);
            await SetContentHeadersAsync(context, upstreamRequest);
        }

        protected virtual Task SetContentBodyAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            var body = context.Request.Body;
            if (!body.CanRead)
            {
                Logger.LogDebug("The inbound downstream request does not have a readable body.");
                return Task.CompletedTask;
            }

            if (body.CanSeek && body.Position != 0)
            {
                Logger.LogDebug("The inbound downstream request has a seek-able body stream. Resetting the stream to the beginning.");
                body.Position = 0;
            }

            upstreamRequest.Content = new StreamContent(context.Request.Body);
            return Task.CompletedTask;
        }

        protected virtual Task SetContentHeadersAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            if (Options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders)
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
            HttpContext context,
            HttpRequestMessage upstreamRequest,
            string name,
            params string[] downstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, downstreamValues, out var upstreamValues) && upstreamValues != null)
            {
                upstreamRequest.Content.Headers.TryAddWithoutValidation(name, upstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by the {1}.{2}.",
                name,
                nameof(IRequestContentHeaderValuesProvider),
                nameof(IRequestContentHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
