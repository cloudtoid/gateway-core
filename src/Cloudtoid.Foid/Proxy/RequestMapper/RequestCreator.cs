namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class RequestCreator : IRequestCreator
    {
        private static readonly IReadOnlyDictionary<string, HttpMethod> HttpMethods = new Dictionary<string, HttpMethod>(StringComparer.OrdinalIgnoreCase)
        {
            { HttpMethod.Get.Method, HttpMethod.Get },
            { HttpMethod.Post.Method, HttpMethod.Post },
            { HttpMethod.Options.Method, HttpMethod.Options },
            { HttpMethod.Head.Method, HttpMethod.Head },
            { HttpMethod.Delete.Method, HttpMethod.Delete },
            { HttpMethod.Patch.Method, HttpMethod.Patch },
            { HttpMethod.Put.Method, HttpMethod.Put },
            { HttpMethod.Trace.Method, HttpMethod.Trace },
        };

        private readonly IUriRewriter uriRewriter;
        private readonly IHeaderSetter headerSetter;
        private readonly ILogger<RequestCreator> logger;

        public RequestCreator(
            IUriRewriter uriRewriter,
            IHeaderSetter headerSetter,
            ILogger<RequestCreator> logger)
        {
            this.uriRewriter = CheckValue(uriRewriter, nameof(uriRewriter));
            this.headerSetter = CheckValue(headerSetter, nameof(headerSetter));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public async Task<HttpRequestMessage> CreateRequestAsync(HttpContext context)
        {
            CheckValue(context, nameof(context));

            context.RequestAborted.ThrowIfCancellationRequested();

            var uri = await uriRewriter
                .RewriteUriAsync(context)
                .TraceOnFaulted(logger, "Failed to rewrite a URI", context.RequestAborted);

            var request = context.Request;
            var message = new HttpRequestMessage
            {
                Method = GetHttpMethod(request.Method),
                RequestUri = uri,
            };

            await headerSetter
                .SetHeadersAsync(context, message)
                .TraceOnFaulted(logger, "Failed to set the headers", context.RequestAborted);

            SetContent(request, message);
            return message;
        }

        private static HttpMethod GetHttpMethod(string method)
            => HttpMethods.TryGetValue(method, out var m) ? m : new HttpMethod(method);

        private void SetContent(HttpRequest request, HttpRequestMessage message)
        {
            if (request.Body is null || !request.Body.CanRead || request.ContentLength <= 0)
                return;

            if (request.Body.CanSeek)
                request.Body.Position = 0;

            message.Content = new StreamContent(request.Body);
        }
    }
}
