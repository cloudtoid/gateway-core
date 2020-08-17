using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudtoid.GatewayCore.Upstream
{
    /// <summary>
    /// By implementing this interface, one can control how an outbound upstream request is sent over the wire.
    /// If your intent is to change or control the HTTP call (such as adding an error handler), you can achieve that in a more standardize way by configuring a named HTTP client.
    /// The name of your configured HTTP client must be set on the sender section of the <see cref="GatewayOptions"/>.
    /// For more information on this pattern, please see <a href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests">here</a> for more information.
    /// </summary>
    public interface IRequestSender
    {
        Task<HttpResponseMessage> SendAsync(
            ProxyContext context,
            HttpRequestMessage upstreamMessage,
            CancellationToken cancellationToken);
    }
}
