namespace Cloudtoid.Foid.Proxy
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class ProxyMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IRequestCreator requestCreator;
        private readonly IRequestSender sender;
        private readonly ILogger<ProxyMiddleware> logger;

        public ProxyMiddleware(
            RequestDelegate next,
            IRequestCreator requestCreator,
            IRequestSender sender,
            ILogger<ProxyMiddleware> logger)
        {
            this.next = CheckValue(next, nameof(next));
            this.requestCreator = CheckValue(requestCreator, nameof(requestCreator));
            this.sender = CheckValue(sender, nameof(sender));
            this.logger = CheckValue(logger, nameof(logger));
        }

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Implementing a delegate that is defined in ASP.NET with this exact signature")]
        public async Task Invoke(HttpContext context)
        {
            CheckValue(context, nameof(context));

            // TODO: Add logging (debug and also errors)

            var request = await requestCreator.CreateRequestAsync(context);

            // Need a better cancellation token here with timeout that is linked to RequestAborted too
            await sender.SendAsync(request, context.RequestAborted);

            await next.Invoke(context);
        }
    }
}
