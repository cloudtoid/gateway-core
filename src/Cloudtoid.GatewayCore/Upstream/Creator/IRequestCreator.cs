using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudtoid.GatewayCore.Upstream
{
    public interface IRequestCreator
    {
        Task<HttpRequestMessage> CreateRequestAsync(
            ProxyContext context,
            CancellationToken cancellationToken);
    }
}
