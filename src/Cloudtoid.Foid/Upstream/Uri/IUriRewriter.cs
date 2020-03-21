namespace Cloudtoid.Foid.Upstream
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUriRewriter
    {
        Task<Uri> RewriteUriAsync(
            ProxyContext context,
            CancellationToken cancellationToken);
    }
}
