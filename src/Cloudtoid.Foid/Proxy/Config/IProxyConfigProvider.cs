namespace Cloudtoid.Foid.Proxy
{
    using System;
    using Microsoft.AspNetCore.Http;

    public interface IProxyConfigProvider
    {
        TimeSpan GetTotalTimeout(HttpContext context);
    }
}
