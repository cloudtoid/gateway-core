namespace Cloudtoid.Foid.Upstream
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Expression;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
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
            var toExpression = context.ProxySettings.To;
            var to = evaluator.Evaluate(context, toExpression);

            string scheme;
            HostString host;
            PathString toPath;
            QueryString toQueryString;
            FragmentString fragment;

            try
            {
                UriHelper.FromAbsolute(
                    to,
                    out scheme,
                    out host,
                    out toPath,
                    out toQueryString,
                    out fragment);
            }
            catch (FormatException ufe)
            {
                var message = $"Using '{context.ProxySettings.To}' expression to rewrite request URL '{context.Request.Path}'. However, the rewritten URL is not a valid URL format.";
                logger.LogError(ufe, message);
                throw new UriFormatException(message, ufe);
            }

            if (!Uri.CheckSchemeName(scheme))
                throw new UriFormatException($"The HTTP scheme '{scheme}' specified by '{toExpression}' expression is invalid.");

            if (!host.HasValue || Uri.CheckHostName(host.Value) == UriHostNameType.Unknown)
                throw new UriFormatException($"The URL host '{host}' specified by '{toExpression}' expression is invalid.");

            var path = toPath + context.Route.PathSuffix;
            var queryString = context.Request.QueryString + toQueryString;

            var url = UriHelper.BuildAbsolute(
                scheme,
                host,
                PathString.Empty,
                path,
                queryString,
                fragment);

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return Task.FromResult(uri);

            throw new UriFormatException($"The URL '{url}' specified by '{toExpression}' expression is an invalid absolute HTTP URL.");
        }
    }
}