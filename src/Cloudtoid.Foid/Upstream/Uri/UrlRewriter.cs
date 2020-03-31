namespace Cloudtoid.Foid.Upstream
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class UrlRewriter : IUrlRewriter
    {
        public Task<Uri> RewriteUrlAsync(ProxyContext context, CancellationToken cancellationToken)
            => throw new NotImplementedException($"Please implement {nameof(IUrlRewriter)} and register it with DI as a transient object.");
    }
}