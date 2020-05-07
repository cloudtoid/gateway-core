namespace Cloudtoid.GatewayCore.Upstream
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore.Expression;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.Extensions.Logging;
    using static Contract;

    /// <inheritdoc/>
    internal sealed class UpstreamUrlCreator : IUpstreamUrlCreator
    {
        private readonly IExpressionEvaluator evaluator;
        private readonly ILogger<UpstreamUrlCreator> logger;

        public UpstreamUrlCreator(
            IExpressionEvaluator evaluator,
            ILogger<UpstreamUrlCreator> logger)
        {
            this.evaluator = CheckValue(evaluator, nameof(evaluator));
            this.logger = CheckValue(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public Task<Uri> CreateAsync(ProxyContext context, CancellationToken cancellationToken)
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

            if (!host.HasValue || Uri.CheckHostName(GetHostValue(host.Value)) == UriHostNameType.Unknown)
                throw new UriFormatException($"The URL host '{host}' specified by '{toExpression}' expression is invalid.");

            var path = ConcatPathWithSuffix(toPath, context.Route.PathSuffix);
            var queryString = toQueryString + context.Request.QueryString;

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

        // internal only for testing
        internal static PathString ConcatPathWithSuffix(PathString path, string suffix)
        {
            if (path.HasValue)
            {
                return path.Value[^1] == '/' && suffix.Length > 0 && suffix[0] == '/'
                    ? new PathString(path.Value + suffix.Substring(1))
                    : new PathString(path.Value + suffix);
            }

            return suffix.Length > 0 && suffix[0] == '/'
                ? new PathString(suffix)
                : new PathString("/" + suffix);
        }

        private static string GetHostValue(string host)
        {
            // remove the port number

            var index = host.Length - 1;
            while (index > -1 && char.IsNumber(host[index]))
                index--;

            if (index > -1 && host[index] == ':')
                return host.Substring(0, index);

            return host;
        }
    }
}