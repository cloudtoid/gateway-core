namespace Cloudtoid.Foid.Upstream
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// By implementing this interface, one can control how an outbound upstream request is sent over the wire.
    /// If your intent is to change or control the HTTP call (such as adding an error handler), you can achieve that in a more standardize way by configuring a named HTTP client.
    /// The name of your configured HTTP client should be passed to <see cref="DependencyInjection.AddFoidProxy(IServiceCollection, string)"/>.
    /// For more information on this pattern, please see <a href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests">here</a> for more information.
    /// </summary>
    public interface IRequestSender
    {
        Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage upstreamMessage,
            CancellationToken cancellationToken);
    }
}
