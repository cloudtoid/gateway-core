namespace Cloudtoid.Foid.Headers
{
    internal static class ContentHeaders
    {
        private const string ContentHeaderPrefix = "Content-";

        internal static bool IsContentHeader(string headerName)
            => headerName.StartsWithOrdinalIgnoreCase(ContentHeaderPrefix);
    }
}
