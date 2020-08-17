using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudtoid.GatewayCore.Downstream
{
    public interface IResponseSender
    {
        Task SendResponseAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken);
    }
}
