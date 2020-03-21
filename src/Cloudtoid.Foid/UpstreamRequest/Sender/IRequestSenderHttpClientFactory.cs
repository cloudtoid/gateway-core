namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;

    internal interface IRequestSenderHttpClientFactory
    {
        HttpClient CreateClient();
    }
}
