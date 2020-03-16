namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IUriRewriter
    {
        Task<Uri> RewriteUriAsync(
            HttpContext context,
            CancellationToken cancellationToken);
    }
}
