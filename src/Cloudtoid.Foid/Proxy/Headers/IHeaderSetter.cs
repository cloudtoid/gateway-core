namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IHeaderSetter
    {
        Task SetHeadersAsync(HttpContext context, HttpRequestMessage newMessage);
    }
}
