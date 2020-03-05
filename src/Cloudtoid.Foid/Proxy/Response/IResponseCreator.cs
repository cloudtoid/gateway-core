namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IResponseCreator
    {
        Task<HttpRequestMessage> CreateResponseAsync(HttpContext context, HttpResponseMessage upstreamResponse);
    }
}
