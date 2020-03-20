namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IResponseSender
    {
        Task SendResponseAsync(
            CallContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken);
    }
}
