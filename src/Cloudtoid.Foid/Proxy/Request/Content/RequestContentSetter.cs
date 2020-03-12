namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    /// <summary>
    /// By inheriting from this class, one can fully control the outbound upstream request content and the content headers. Please, consider the following extensibility points:
    /// 1. Inherit from <see cref="RequestContentHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IRequestContentHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="RequestContentSetter"/>, override its methods, and register it with DI; or
    /// 4. Finally, you can implement <see cref="IRequestContentSetter"/> and register it with DI; or
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IRequestHeaderValuesProvider, MyRequestHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IRequestHeaderSetter, MyRequestHeaderSetter>()</c>
    /// </summary>
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
                Logger.LogDebug("The inbound downstream request has a seekable body stream. Resetting the stream to the begining.");
                body.Position = 0;
            }

            upstreamRequest.Content = new StreamContent(context.Request.Body);
            return Task.CompletedTask;
        }

        protected virtual Task SetContentHeadersAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            var request = context.Request;

            foreach (var header in request.Headers)
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
                "Header '{0}' is not added. This was was instructed by the {1}.{2}.",
                name,
                nameof(IRequestContentHeaderValuesProvider),
                nameof(IRequestContentHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
