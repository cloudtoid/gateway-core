namespace Cloudtoid.Foid.Upstream
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Expression;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class UrlRewriter : IUrlRewriter
    {
        private readonly IExpressionEvaluator evaluator;
        private readonly ILogger<UrlRewriter> logger;

        public UrlRewriter(
            IExpressionEvaluator evaluator,
            ILogger<UrlRewriter> logger)
        {
            this.evaluator = CheckValue(evaluator, nameof(evaluator));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public Task<Uri> RewriteUrlAsync(ProxyContext context, CancellationToken cancellationToken)
        {
            var url = evaluator.Evaluate(context, context.ProxySettings.To) + context.Route.PathSuffix;
            if (context.Request.QueryString.HasValue)
                url += context.Request.QueryString.Value;

            try
            {
                return Task.FromResult(new Uri(url));
            }
            catch (FormatException ufe)
            {
                logger.LogError(
                    ufe,
                    "Using '{0}' expression to rewrite request URL path '{1}' to '{2}'. However, the rewritten URL is not a valid URL format.",
                    context.ProxySettings.To,
                    context.Request.Path,
                    url);

                throw;
            }
        }
    }
}