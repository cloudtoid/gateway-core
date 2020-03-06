namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IResponseHeaderSetter
    {
        Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse);
    }
}
