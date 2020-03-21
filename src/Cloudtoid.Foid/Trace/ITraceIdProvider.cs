namespace Cloudtoid.Foid.Trace
{
    public interface ITraceIdProvider
    {
        /// <summary>
        /// Returns the name of the HTTP header to be used for correlation-id.
        /// This method is only called once per request.
        /// </summary>
        string GetCorrelationIdHeader(ProxyContext context);

        /// <summary>
        /// Returns the correlation-id of this activity.
        /// Please note that the correlation-id can be specified by the client using a header. If not specified, a new correlation-id is created.
        /// This method is only called once per request.
        /// </summary>
        string GetOrCreateCorrelationId(ProxyContext context);

        /// <summary>
        /// Returns the call-id of this particular call.
        /// Please note that the call-id is always new and unique per each inbound downstream request. This cannot be specified by the client.
        /// This method is only called once per request.
        /// </summary>
        string CreateCallId(ProxyContext context);
    }
}
