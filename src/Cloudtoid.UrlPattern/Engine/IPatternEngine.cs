namespace Cloudtoid.UrlPattern
{
    using System.Diagnostics.CodeAnalysis;

    public interface IPatternEngine
    {
        bool TryMatch(
            string pattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match,
            [NotNullWhen(false)] out string? why);

        PatternMatchResult Match(
            string pattern,
            string path);
    }
}
