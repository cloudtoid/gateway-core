namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRequestSender
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken);
    }
}
