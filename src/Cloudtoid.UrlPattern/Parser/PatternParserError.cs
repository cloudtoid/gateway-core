namespace Cloudtoid.UrlPattern
{
    internal sealed class PatternParserError
    {
        internal PatternParserError(string message, int? location = null)
        {
            Message = message;
            Location = location;
        }

        internal string Message { get; }

        internal int? Location { get; }
    }
}
