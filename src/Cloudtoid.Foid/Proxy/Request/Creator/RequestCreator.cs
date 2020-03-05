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
        private readonly IRequestHeaderSetter headerSetter;
        private readonly ILogger<RequestCreator> logger;

        public RequestCreator(
            IUriRewriter uriRewriter,
            IRequestHeaderSetter headerSetter,
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

            logger.LogDebug("Creating an outgoing upstream HTTP request based on the incoming downstream HTTP request.");

            var request = context.Request;
            var message = new HttpRequestMessage
            {
                Method = GetHttpMethod(request.Method),
                RequestUri = await RewriteUriAsync(context),
            };

            await SetHeadersAsync(context, message);
            SetContent(request, message);

            logger.LogDebug("Creating an outgoing upstream HTTP request based on the incoming downstream HTTP request.");

            return message;
        }

        private static HttpMethod GetHttpMethod(string method)
            => HttpMethods.TryGetValue(method, out var m) ? m : new HttpMethod(method);

        private async Task<Uri> RewriteUriAsync(HttpContext context)
        {
            logger.LogDebug("Rewrote the uri by calling an instance of {0}", uriRewriter.GetType().FullName);

            var uri = await uriRewriter
                .RewriteUriAsync(context)
                .TraceOnFaulted(logger, "Failed to rewrite a URI", context.RequestAborted);

            logger.LogDebug("Rewrote the uri by calling an instance of {0}", uriRewriter.GetType().FullName);

            return uri;
        }

        private async Task SetHeadersAsync(HttpContext context, HttpRequestMessage message)
        {
            logger.LogDebug("Appending HTTP headers to the outgoing upstream request");

            await headerSetter
                .SetHeadersAsync(context, message)
                .TraceOnFaulted(logger, "Failed to set the headers", context.RequestAborted);

            logger.LogDebug("Appended HTTP headers to the outgoing upstream request");
        }

        private void SetContent(HttpRequest request, HttpRequestMessage message)
        {
            logger.LogDebug("Transferring the content of the incoming downstream request to the outgoing upstream request");

            if (request.Body is null || !request.Body.CanRead || request.ContentLength <= 0)
            {
                logger.LogDebug("The incoming downstream request does not have a body, it is not readable, or its content length is zero.");
                return;
            }

            if (request.Body.CanSeek && request.Body.Position != 0)
            {
                logger.LogDebug("The incoming downstream request has a seekable body stream. Resetting the stream to the begining.");
                request.Body.Position = 0;
            }

            message.Content = new StreamContent(request.Body);

            logger.LogDebug("Transferred the content of the incoming downstream request to the outgoing upstream request");
        }
    }
}
