namespace Cloudtoid.UrlPattern
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    internal interface IPatternParser
    {
        bool TryParse(
            string pattern,
            [NotNullWhen(true)] out PatternNode? parsedPattern,
            [NotNullWhen(false)] out IReadOnlyList<PatternParserError>? errors);
    }
}
