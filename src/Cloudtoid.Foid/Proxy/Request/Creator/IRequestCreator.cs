namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IRequestCreator
    {
        Task<HttpRequestMessage> CreateRequestAsync(
            HttpContext context,
            CancellationToken cancellationToken);
    }
}
