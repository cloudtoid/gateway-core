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

            await requestCreator.CreateRequestAsync(context);
            //// await sender.SendAsync();

            await next.Invoke(context);
        }
    }
}
