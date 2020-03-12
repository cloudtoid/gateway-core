namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    /// <summary>
    /// By inheriting from this class, one can have full control over the outbound downstream response content, content headers, and trailing headers. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// 1. Inherit from <see cref="ResponseContentHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="IResponseContentHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="ResponseContentSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="IResponseContentSetter"/> and register it with DI
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<IResponseHeaderValuesProvider, MyResponseHeaderValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<IResponseHeaderSetter, MyResponseHeaderSetter>()</c>
    /// </summary>s
    public class ResponseContentSetter : IResponseContentSetter
    {
        public ResponseContentSetter(
            IResponseContentHeaderValuesProvider provider,
            ILogger<ResponseContentSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Logger = CheckValue(logger, nameof(logger));
        }

        protected IResponseContentHeaderValuesProvider Provider { get; }

        protected ILogger<ResponseContentSetter> Logger { get; }

        public virtual async Task SetContentAsync(
            HttpContext context,
            HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            var body = context.Response.Body;
            if (body is null)
            {
                Logger.LogDebug("The inbound downstream request does not have a content body.");
                return;
            }

            await SetContentBodyAsync(context, upstreamResponse);
            await SetContentHeadersAsync(context, upstreamResponse);
        }

        protected virtual Task SetContentBodyAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            var body = context.Response.Body;
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

            upstreamResponse.Content = new StreamContent(context.Response.Body);
            return Task.CompletedTask;
        }

        protected virtual Task SetContentHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            var request = context.Response;

            foreach (var header in request.Headers)
            {
                var name = header.Key;
                if (!HeaderTypes.IsContentHeader(name))
                    continue;

                AddHeaderValues(
                    context,
                    upstreamResponse,
                    name,
                    header.Value);
            }

            return Task.CompletedTask;
        }

        protected virtual void AddHeaderValues(
            HttpContext context,
            HttpResponseMessage upstreamResponse,
            string name,
            params string[] downstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, downstreamValues, out var upstreamValues) && upstreamValues != null)
            {
                upstreamResponse.Content.Headers.TryAddWithoutValidation(name, upstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was was instructed by the {1}.{2}.",
                name,
                nameof(IResponseContentHeaderValuesProvider),
                nameof(IResponseContentHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
