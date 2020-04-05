namespace Cloudtoid.GatewayCore.Upstream
{
    using System.Net.Http;

    internal interface IRequestSenderHttpClientFactory
    {
        HttpClient CreateClient();
    }
}
