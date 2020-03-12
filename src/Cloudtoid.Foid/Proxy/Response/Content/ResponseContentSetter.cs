namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
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
        private static readonly ISet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.TransferEncoding,
        };

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

            await SetContentHeadersAsync(context, upstreamResponse);
            await SetContentBodyAsync(context, upstreamResponse);
        }

        protected virtual async Task SetContentBodyAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            var downstreamResponseStream = context.Response.Body;

            // TODO: the current version of HttpContent.CopyToAsync doesn't expose the cancellation-token
            // However, they are working on fixing that. We should modifgy this code and pass in context.RequestAborted
            await upstreamResponse.Content.CopyToAsync(downstreamResponseStream);
            await downstreamResponseStream.FlushAsync(context.RequestAborted);
        }

        protected virtual Task SetContentHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            context.RequestAborted.ThrowIfCancellationRequested();

            var headers = upstreamResponse.Content?.Headers;
            if (headers is null)
                return Task.CompletedTask;

            foreach (var header in headers)
            {
                var name = header.Key;

                if (HeaderTransferBlacklist.Contains(name))
                    continue;

                AddHeaderValues(
                    context,
                    upstreamResponse,
                    name,
                    header.Value.AsArray());
            }

            return Task.CompletedTask;
        }

        protected virtual void AddHeaderValues(
            HttpContext context,
            HttpResponseMessage upstreamResponse,
            string name,
            params string[] upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.Headers[name] = downstreamValues;
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
