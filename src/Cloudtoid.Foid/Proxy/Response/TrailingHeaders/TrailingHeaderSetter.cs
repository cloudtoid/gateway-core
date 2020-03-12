namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;
    using Options = OptionsProvider.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions;

    /// <summary>
    /// By inheriting from this class, one can have full control over the outbound downstream response trailing headers. Please consider the following extensibility points:
    /// 1. Inherit from <see cref="TrailingHeaderValuesProvider"/>, override its methods, and register it with DI; or
    /// 2. Implement <see cref="ITrailingHeaderValuesProvider"/> and register it with DI; or
    /// 3. Inherit from <see cref="TrailingHeaderSetter"/>, override its methods, and register it with DI; or
    /// 4. Implement <see cref="ITrailingHeaderSetter"/> and register it with DI.
    ///
    /// Dependency Injection registrations:
    /// 1. <c>TryAddSingleton<ITrailingHeadersValuesProvider, MyTrailingHeadersValuesProvider>()</c>
    /// 2. <c>TryAddSingleton<ITrailingHeadersSetter, MyTrailingHeadersSetter>()</c>
    /// </summary>
    public class TrailingHeaderSetter : ITrailingHeaderSetter
    {
        private static readonly ISet<string> HeaderTransferBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ProxyHeaderNames.CallId,
        };

        public TrailingHeaderSetter(
            ITrailingHeaderValuesProvider provider,
            ILogger<TrailingHeaderSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Logger = CheckValue(logger, nameof(logger));
        }

        protected ITrailingHeaderValuesProvider Provider { get; }

        protected ILogger<TrailingHeaderSetter> Logger { get; }

        public virtual Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            if (upstreamResponse.TrailingHeaders is null)
                return Task.CompletedTask;

            if (!ResponseTrailerExtensions.SupportsTrailers(context.Response))
                return Task.CompletedTask;

            var headers = upstreamResponse.TrailingHeaders;
            foreach (var header in headers)
                AddHeaderValues(context, header.Key, header.Value.AsArray());

            return Task.CompletedTask;
        }

        protected virtual void AddHeaderValues(
            HttpContext context,
            string name,
            params string[] upstreamValues)
        {
            if (Provider.TryGetHeaderValues(context, name, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.AppendTrailer(name, downstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was was instructed by the {1}.{2}.",
                name,
                nameof(ITrailingHeaderValuesProvider),
                nameof(ITrailingHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
