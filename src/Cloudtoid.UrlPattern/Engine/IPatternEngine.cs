namespace Cloudtoid.UrlPattern
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public interface IPatternEngine
    {
        /// <summary>
        /// Matches the <paramref name="path"/> against the <paramref name="pattern"/>.
        /// If a match could not be made, <paramref name="why"/> specifies the reason for it. It is typically because
        /// <paramref name="path"/> is not a match, but sometimes it could be that the processing timed out.
        /// <paramref name="pattern"/> is compiled and cached on the very first call.
        /// </summary>
        bool TryMatch(
            string pattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match,
            [NotNullWhen(false)] out string? why);

        /// <summary>
        /// Matches the <paramref name="path"/> against the <paramref name="pattern"/>.
        /// </summary>
        /// <exception cref="PatternException">is thrown if the compilation of the pattern times out or if the pattern is invalid.</exception>
        PatternMatchResult Match(
            string pattern,
            string path);

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

        /// <summary>
        /// Parses and compiles the URL pattern specified by the <paramref name="pattern"/> parameter.
        /// </summary>
        bool TryCompile(
            string pattern,
            [NotNullWhen(true)] out CompiledPattern? compiledPattern,
            [NotNullWhen(false)] out IReadOnlyList<PatternCompilerError>? errors);
    }
}
