namespace Cloudtoid.Foid.Routes.Pattern
{
    using System.Diagnostics.CodeAnalysis;

    internal interface IPatternMatcher
    {
        bool TryMatch(
            CompiledPattern pattern,
            string route,
            [NotNullWhen(true)] out PatternMatchResult? match);
    }
}
