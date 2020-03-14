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
    using static Contract;
    using Options = Options.OptionsProvider.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions;

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
            Names.CallId,
        };

        private readonly HeaderSanetizer sanetizer;

        public TrailingHeaderSetter(
            ITrailingHeaderValuesProvider provider,
            OptionsProvider options,
            ILogger<TrailingHeaderSetter> logger)
        {
            Provider = CheckValue(provider, nameof(provider));
            Options = CheckValue(options, nameof(options));
            Logger = CheckValue(logger, nameof(logger));
            sanetizer = new HeaderSanetizer(logger);
        }

        protected ITrailingHeaderValuesProvider Provider { get; }

        protected OptionsProvider Options { get; }

        protected ILogger<TrailingHeaderSetter> Logger { get; }

        // Do NOT cache this value. Options react to changes.
        private Options HeaderOptions => Options.Proxy.Downstream.Response.Headers;

        public virtual Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            if (HeaderOptions.IgnoreAllUpstreamHeaders)
                return Task.CompletedTask;

            context.RequestAborted.ThrowIfCancellationRequested();

            if (upstreamResponse.TrailingHeaders is null)
                return Task.CompletedTask;

            if (!ResponseTrailerExtensions.SupportsTrailers(context.Response))
                return Task.CompletedTask;

            var allowHeadersWithEmptyValue = HeaderOptions.AllowHeadersWithEmptyValue;
            var allowHeadersWithUnderscoreInName = HeaderOptions.AllowHeadersWithUnderscoreInName;
            var headers = upstreamResponse.TrailingHeaders;

            foreach (var header in headers)
            {
                if (!sanetizer.IsValid(
                    header.Key,
                    header.Value,
                    allowHeadersWithEmptyValue,
                    allowHeadersWithUnderscoreInName))
                    continue;

                AddHeaderValues(context, header.Key, header.Value.AsArray());
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
                context.Response.AppendTrailer(name, downstreamValues);
                return;
            }

            Logger.LogInformation(
                "Header '{0}' is not added. This was instructed by the {1}.{2}.",
                name,
                nameof(ITrailingHeaderValuesProvider),
                nameof(ITrailingHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
