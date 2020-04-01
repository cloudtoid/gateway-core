namespace Cloudtoid.UrlPattern
{
    using System.Diagnostics.CodeAnalysis;

    public interface IPatternMatcher
    {
        /// <summary>
        /// Matches the <paramref name="path"/> against the <paramref name="compiledPattern"/>.
        /// If a match could not be made, <paramref name="why"/> specifies the reason for it. It is typically because
        /// <paramref name="path"/> is not a match, but sometimes it could be that the processing timed out.
        /// </summary>
        bool TryMatch(
            CompiledPattern compiledPattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match,
            [NotNullWhen(false)] out string? why);
    }
}
