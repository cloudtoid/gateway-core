namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IResponseSender
    {
        Task SendResponseAsync(
            HttpContext context,
            HttpResponseMessage upstreamResponse);
    }
}
