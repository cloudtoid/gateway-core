namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Headers;
    using Cloudtoid.Foid.Options;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using static Contract;
    using Options = Options.OptionsProvider.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions;

    /// <summary>
    /// By inheriting from this class, one can have full control over the outbound downstream response content and content header. However, a fully functioning implementation is nontrivial. Therefore, before implementing this interface, consider the following extensibility points:
    /// <list type="number">
    /// <item><description>Inherit from <see cref="ResponseContentHeaderValuesProvider"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseContentHeaderValuesProvider"/> and register it with DI; or</description></item>
    /// <item><description>Inherit from <see cref="ResponseContentSetter"/>, override its methods, and register it with DI; or</description></item>
    /// <item><description>Implement <see cref="IResponseContentSetter"/> and register it with DI</description></item>
    /// </list>
    /// </summary>
    /// <example>
    /// Dependency Injection registrations:
    /// <list type="bullet">
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseContentHeaderValuesProvider"/>, MyResponseContentHeaderValuesProvider&gt;()</c></description></item>
    /// <item><description><c>TryAddSingleton&lt;<see cref="IResponseContentSetter"/>, MyResponseContentSetter&gt;()</c></description></item>
    /// </list>
    /// </example>
    public class ResponseContentSetter : IResponseContentSetter
    {
        private static readonly ISet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.TransferEncoding,
        };

        private readonly HeaderSanetizer sanetizer;

        public ResponseContentSetter(
            IResponseContentHeaderValuesProvider provider,
            OptionsProvider options,
            ILogger<ResponseContentSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Options = CheckValue(options, nameof(options));
            Logger = CheckValue(logger, nameof(logger));
            sanetizer = new HeaderSanetizer(logger);
        }

        protected IResponseContentHeaderValuesProvider Provider { get; }

        protected OptionsProvider Options { get; }

        protected ILogger<ResponseContentSetter> Logger { get; }

        // Do NOT cache this value. Options react to changes.
        private Options HeaderOptions => Options.Proxy.Downstream.Response.Headers;

        public virtual async Task SetContentAsync(
            HttpContext context,
            HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            if (upstreamResponse.Content is null)
            {
                Logger.LogDebug("The inbound upstream response does not have a content body.");
                return;
            }

            if (context.Response.ContentLength <= 0)
            {
                Logger.LogDebug("The inbound upstream response has a content length of zero.");
                return;
            }

            await SetContentHeadersAsync(context, upstreamResponse);
            await SetContentBodyAsync(context, upstreamResponse);
            await SetContentHeadersAsync(context, upstreamResponse);
        }

        protected virtual async Task SetContentBodyAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            var downstreamResponseStream = context.Response.Body;

            // TODO: the current version of HttpContent.CopyToAsync doesn't expose the cancellation-token
            // However, they are working on fixing that. We should modify this code and pass in context.RequestAborted
            await upstreamResponse.Content.CopyToAsync(downstreamResponseStream);
            await downstreamResponseStream.FlushAsync(context.RequestAborted);
        }

        protected virtual Task SetContentHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (HeaderOptions.IgnoreAllUpstreamHeaders)
                return Task.CompletedTask;

            context.RequestAborted.ThrowIfCancellationRequested();

            var headers = upstreamResponse.Content?.Headers;
            if (headers is null)
                return Task.CompletedTask;

            var allowHeadersWithEmptyValue = HeaderOptions.AllowHeadersWithEmptyValue;
            var allowHeadersWithUnderscoreInName = HeaderOptions.AllowHeadersWithUnderscoreInName;

            foreach (var header in headers)
            {
                var name = header.Key;

                if (!sanetizer.IsValid(
                    name,
                    header.Value,
                    allowHeadersWithEmptyValue,
                    allowHeadersWithUnderscoreInName))
                    continue;

                if (HeaderTransferBlacklist.Contains(name))
                    continue;

                AddHeaderValues(
                    context,
                    name,
                    header.Value.AsArray());
            }

            return Task.CompletedTask;
        }

        protected virtual void AddHeaderValues(
            HttpContext context,
            string name,
            params string[] upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.Headers[name] = downstreamValues;
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by the {1}.{2}.",
                name,
                nameof(IResponseContentHeaderValuesProvider),
                nameof(IResponseContentHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
