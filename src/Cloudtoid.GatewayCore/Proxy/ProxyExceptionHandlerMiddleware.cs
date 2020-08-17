using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Proxy
{
    internal sealed class ProxyExceptionHandlerMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ProxyExceptionHandlerMiddleware> logger;

        public ProxyExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<ProxyExceptionHandlerMiddleware> logger)
        {
            this.next = CheckValue(next, nameof(next));
            this.logger = CheckValue(logger, nameof(logger));
        }

        [SuppressMessage("Style", "VSTHRD200:Use Async suffix for async methods", Justification = "Implementing an ASP.NET middleware. This signature cannot be changed.")]
        public async Task Invoke(HttpContext context)
        {
            CheckValue(context, nameof(context));

            try
            {
                await next.Invoke(context);
            }
            catch (OperationCanceledException)
            {
                SetStatusCode(context, HttpStatusCode.GatewayTimeout);
            }
            catch (ProxyException pe)
            {
                logger.LogError(pe, "{0} - Failed to proxy a request.", (int)pe.StatusCode);
                SetStatusCode(context, pe.StatusCode);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.LogError(ex, "500 - Failed to proxy a request.");
                SetStatusCode(context, HttpStatusCode.InternalServerError);
            }
        }

        private static void SetStatusCode(HttpContext context, HttpStatusCode statusCode)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = (int)statusCode;
        }
    }
}
