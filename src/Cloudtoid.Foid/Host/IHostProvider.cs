namespace Cloudtoid.Foid.Host
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can override the HOST header of the outbound upstream request.
    /// You can also inherit from <see cref="HostProvider"/> and register it with DI.
    /// </summary>
    public interface IHostProvider
    {
        /// <summary>
        /// Returns the value that should be used as the HOST header of the outbound upstream request
        /// </summary>
        string GetHost(HttpContext context);
    }
}
