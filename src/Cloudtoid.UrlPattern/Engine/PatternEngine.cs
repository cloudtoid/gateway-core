namespace Cloudtoid.UrlPattern
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using static Contract;

    public sealed class PatternEngine : IPatternEngine
    {
        private static readonly PatternEngineOptions DefaultOptions = new PatternEngineOptions();

        private readonly PatternEngineOptions options;
        private readonly IPatternCompiler compiler;
        private readonly IPatternMatcher matcher;
        private readonly IUrlPathNormalizer normalizer;
        private readonly ConcurrentDictionary<string, CompiledPatternInfo> compiledPatterns;

        public PatternEngine(PatternEngineOptions? options = null)
        {
            this.options = options ?? DefaultOptions;

            var resolver = new PatternTypeResolver();
            var parser = new PatternParser();
            var validator = new PatternValidator();
            compiler = new PatternCompiler(resolver, parser, validator);
            matcher = new PatternMatcher();
            normalizer = new UrlPathNormalizer();
            compiledPatterns = new ConcurrentDictionary<string, CompiledPatternInfo>(StringComparer.Ordinal);
        }

        public bool TryMatch(
            string pattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match,
            [NotNullWhen(false)] out string? why)
        {
            CheckValue(pattern, nameof(pattern));
            CheckValue(path, nameof(path));

            if (!TryCompile(pattern, out var compiledPattern, out why))
            {
                match = null;
                return false;
            }

            path = normalizer.Normalize(path);
            return TryMatch(compiledPattern, path, out match, out why);
        }

        public PatternMatchResult Match(string pattern, string path)
        {
            CheckValue(pattern, nameof(pattern));
            CheckValue(path, nameof(path));

            if (TryMatch(pattern, path, out var match, out var why))
                return match;

            throw new PatternException(why);
        }

        private bool TryCompile(
            string pattern,
            [NotNullWhen(true)] out CompiledPattern? compiledPattern,
            [NotNullWhen(false)] out string? error)
        {
            if (compiledPatterns.TryGetValue(pattern, out var info))
            {
                error = info.Error;
                if (error is null)
                {
                    compiledPattern = CheckValue(info.CompiledPattern, nameof(info.CompiledPattern));
                    return true;
                }
                else
                {
                    compiledPattern = null;
                    return false;
                }
            }

            if (!compiler.TryCompile(pattern, out compiledPattern, out var errors))
            {
                error = GetCompileErrorMessage(errors);
                compiledPatterns.TryAdd(pattern, new CompiledPatternInfo(error));
                return false;
            }

            error = null;
            compiledPatterns.TryAdd(pattern, new CompiledPatternInfo(compiledPattern));
            return true;
        }

        private bool TryMatch(
            CompiledPattern compiledPattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match,
            [NotNullWhen(false)] out string? why)
        {
            return matcher.TryMatch(compiledPattern, path, out match, out why);
        }

        private static string GetCompileErrorMessage(IReadOnlyList<PatternCompilerError> errors)
        {
            var builder = new StringBuilder("Failed to compile the pattern with the following errors:").AppendLine();
            foreach (var error in errors)
                builder.AppendLine(error.ToString());

            return builder.ToString();
        }

        private sealed class CompiledPatternInfo
        {
            internal CompiledPatternInfo(CompiledPattern compiledPattern)
            {
                CompiledPattern = compiledPattern;
            }

            internal CompiledPatternInfo(string error)
            {
                Error = error;
            }

            internal CompiledPattern? CompiledPattern { get; }

            internal string? Error { get; }
        }
    }
}
