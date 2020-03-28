namespace Cloudtoid.UrlPattern
{
    using System.Diagnostics.CodeAnalysis;

    public interface IPatternMatcher
    {
        bool TryMatch(
            CompiledPattern pattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match);
    }
}
