namespace Cloudtoid.GatewayCore.Upstream
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRequestCreator
    {
        Task<HttpRequestMessage> CreateRequestAsync(
            ProxyContext context,
            CancellationToken cancellationToken);
    }
}
