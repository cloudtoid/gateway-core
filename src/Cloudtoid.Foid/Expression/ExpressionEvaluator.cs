namespace Cloudtoid.Foid
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using static Contract;

    internal sealed class ExpressionEvaluator : IExpressionEvaluator
    {
        private static readonly Dictionary<string, Func<Context, string?>> Actions = new Dictionary<string, Func<Context, string?>>(StringComparer.OrdinalIgnoreCase)
        {
            { VariableNames.ContentLength,  GetContentLength },
            { VariableNames.ContentType,  GetContentType },
            { VariableNames.CorrelationId,  GetCorrelationId },
            { VariableNames.CallId,  GetCallId },
            { VariableNames.Host,  GetHost },
            { VariableNames.QueryString,  GetQueryString },
            { VariableNames.RequestUri,  GetRequestUri },
            { VariableNames.RemoteAddress,  GetRemoteAddress },
            { VariableNames.RemotePort,  GetRemotePort },
            { VariableNames.RequestMethod,  GetRequestMethod },
            { VariableNames.Scheme,  GetScheme },
            { VariableNames.ServerAddress,  GetServerAddress },
            { VariableNames.ServerName,  GetServerName },
            { VariableNames.ServerPort,  GetServerPort },
            { VariableNames.ServerProtocol,  GetServerProtocol },
        };

        private readonly ITraceIdProvider traceIdProvider;
        private readonly IHostProvider hostProvider;

        public ExpressionEvaluator(
            ITraceIdProvider traceIdProvider,
            IHostProvider hostProvider)
        {
            this.traceIdProvider = CheckValue(traceIdProvider, nameof(traceIdProvider));
            this.hostProvider = CheckValue(hostProvider, nameof(hostProvider));
        }

        public string? Evaluate(HttpContext context, string? expression)
        {
            var expr = expression;
            if (expr is null)
                return null;

            expr = expr.Trim();
            if (expr.Length == 0)
                return expression;

            if (!expr.StartsWith('$'))
                return expression;

            expr = expr.Substring(1);
            if (expr.Length == 0)
                return expression;

            int i = 0;
            var len = expr.Length;
            while (i++ < len && IsValidVariableChar(expr[i]))
            {
            }

            expr = expr.Substring(0, i);
            if (!Actions.TryGetValue(expr, out var action))
                return expression;

            var c = new Context
            {
                HttpContext = context,
                TraceIdProvider = traceIdProvider,
                HostProvider = hostProvider,
            };

            return action(c);
        }

        /// <summary>
        /// "Content-Length" request header field
        /// </summary>
        private static string? GetContentLength(Context context)
            => context.Request.ContentLength?.ToStringInvariant();

        /// <summary>
        /// "Content-Type" request header field
        /// </summary>
        private static string? GetContentType(Context context)
            => context.Request.ContentType;

        /// <summary>
        /// The value correlation identifier header if not present or a newly generated one.
        /// </summary>
        private static string? GetCorrelationId(Context context)
            => context.TraceIdProvider.GetCorrelationId(context.HttpContext);

        /// <summary>
        /// The value correlation identifier header if not present or a newly generated one.
        /// The default header name of correlation identifier is "x-correlation-id" but this can be changed
        /// using the CorrelationIdHeader option.
        /// </summary>
        private static string? GetCallId(Context context)
            => context.TraceIdProvider.GetCallId(context.HttpContext);

        /// <summary>
        /// The value that should be used as the HOST header on the outgoing upstream request.
        /// </summary>
        private static string? GetHost(Context context)
            => context.HostProvider.GetHost(context.HttpContext);

        /// <summary>
        /// The escaped query string with the leading '?' character
        /// </summary>
        private static string? GetQueryString(Context context)
            => context.Request.QueryString.Value;

        /// <summary>
        /// The full original escaped request URI without the query string portion.
        /// </summary>
        private static string? GetRequestUri(Context context)
            => context.Request.Path.Value;

        /// <summary>
        /// The IP address of the client
        /// </summary>
        private static string? GetRemoteAddress(Context context)
            => context.HttpContext.Connection?.RemoteIpAddress?.ToString();

        /// <summary>
        /// The IP port number of the remote client.
        /// </summary>
        private static string? GetRemotePort(Context context)
            => context.HttpContext.Connection?.RemotePort.ToStringInvariant();

        /// <summary>
        /// The HTTP method of the incoming downstream request
        /// </summary>
        private static string? GetRequestMethod(Context context)
            => context.Request.Method;

        /// <summary>
        /// The shceme (HTTP or HTTPS) used by the incoming downstream request
        /// </summary>
        private static string? GetScheme(Context context)
            => context.Request.Scheme;

        /// <summary>
        /// The IP address of the server which accepted the request
        /// </summary>
        private static string? GetServerAddress(Context context)
            => context.HttpContext.Connection?.LocalIpAddress?.ToString();

        /// <summary>
        /// The IP port number of the server which accepted the request
        /// </summary>
        private static string? GetServerPort(Context context)
            => context.HttpContext.Connection?.LocalPort.ToStringInvariant();

        /// <summary>
        /// The name of the server which accepted the request
        /// </summary>
        private static string? GetServerName(Context context)
            => Environment.MachineName;

        /// <summary>
        /// The protocol of the incoming downstream request, usually “HTTP/1.0”, “HTTP/1.1”, or “HTTP/2.0”
        /// </summary>
        private static string? GetServerProtocol(Context context)
            => context.Request.Protocol;

        private static bool IsValidVariableChar(char c)
        {
            int a = c;
            return (a > 64 && a < 91) || (a > 96 && a < 123) || a == 95;
        }

        internal struct Context
        {
            internal HttpContext HttpContext { get; set; }

            internal ITraceIdProvider TraceIdProvider { get; set; }

            internal IHostProvider HostProvider { get; set; }

            internal HttpRequest Request => HttpContext.Request;
        }
    }
}
