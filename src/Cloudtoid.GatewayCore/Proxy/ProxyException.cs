namespace Cloudtoid.GatewayCore.Proxy
{
    using System;
    using System.Net;
    using static Contract;

    /// <summary>
    /// This exception class should be used to set the status code of the outbound downstream response
    /// to an HTTP status code that indicates a failure.
    /// </summary>
    /// <remarks>
    /// This exception is automatically caught by <see cref="ProxyExceptionHandlerMiddleware"/> and is converted to its HTTP status code.
    /// </remarks>
    public class ProxyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyException"/> class.
        /// </summary>
        /// <param name="statusCode">This should be an HTTP status code that is not in the 200-299 range.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ProxyException(HttpStatusCode statusCode, Exception? innerException = null)
            : base(null, innerException)
        {
            CheckParam(
                !statusCode.IsSuccessStatusCode(),
                nameof(statusCode),
                "A {0} should not specify a successful http status code. Status code = {1}",
                nameof(ProxyException),
                statusCode);

            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
