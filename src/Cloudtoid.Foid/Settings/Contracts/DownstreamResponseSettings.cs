namespace Cloudtoid.Foid.Settings
{
    public sealed class DownstreamResponseSettings
    {
        internal DownstreamResponseSettings(
            DownstreamResponseHeadersSettings headers)
        {
            Headers = headers;
        }

        public DownstreamResponseHeadersSettings Headers { get; }
    }
}