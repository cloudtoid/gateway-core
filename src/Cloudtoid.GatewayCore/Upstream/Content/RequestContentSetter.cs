using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Upstream
{
    /// <summary>
    /// By inheriting from this class, one can have full control over the outbound upstream content and its content headers. Please consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="RequestContentSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IRequestContentSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IRequestContentSetter"/>, MyRequestContentSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public class RequestContentSetter : IRequestContentSetter
    {
        public RequestContentSetter(ILogger<RequestContentSetter> logger)
            => Logger = CheckValue(logger, nameof(logger));

        protected ILogger<RequestContentSetter> Logger { get; }

        /// <inheritdoc/>
        public virtual Task SetContentAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamRequest, nameof(upstreamRequest));

            cancellationToken.ThrowIfCancellationRequested();

            var body = context.Request.Body;
            if (body is null)
            {
                Logger.LogDebug("The inbound downstream request does not have a content body.");
                return Task.CompletedTask;
            }

            if (!context.Request.ContentLength.HasValue)
            {
                Logger.LogDebug("The inbound downstream request does not specify a 'Content-Length'.");
                return Task.CompletedTask;
            }

            if (!body.CanRead)
            {
                Logger.LogError("The inbound downstream request does not have a readable request body.");
                throw new InvalidOperationException("The inbound downstream request does not have a readable request body.");
            }

            if (body.CanSeek && body.Position != 0)
            {
                Logger.LogDebug("The inbound downstream request has a seek-able body stream. Resetting the stream to the beginning.");
                body.Position = 0;
            }

            upstreamRequest.Content = new StreamContent(context.Request.Body);
            return Task.CompletedTask;
        }
    }
}
