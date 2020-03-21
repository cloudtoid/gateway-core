namespace Cloudtoid.Foid.Downstream
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IResponseSender
    {
        Task SendResponseAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken);
    }
}
