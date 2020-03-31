namespace Cloudtoid.Foid.Upstream
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUrlRewriter
    {
        Task<Uri> RewriteUrlAsync(
            ProxyContext context,
            CancellationToken cancellationToken);
    }
}
