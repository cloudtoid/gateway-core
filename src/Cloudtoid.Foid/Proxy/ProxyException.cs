namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Net;

    public class ProxyException : Exception
    {
        public ProxyException(HttpStatusCode statusCode, Exception? innerException = null)
            : base(null, innerException)
        {
            Contract.CheckRange((int)statusCode, 200, 299, nameof(statusCode));
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
