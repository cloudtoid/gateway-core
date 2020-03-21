namespace Cloudtoid.Foid.Upstream
{
    using System.Net.Http;

    internal interface IRequestSenderHttpClientFactory
    {
        HttpClient CreateClient();
    }
}
