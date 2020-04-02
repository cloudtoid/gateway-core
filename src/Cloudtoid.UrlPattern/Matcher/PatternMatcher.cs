namespace Cloudtoid.UrlPattern
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;
    using static Contract;

    internal sealed class PatternMatcher : IPatternMatcher
    {
        private readonly IUrlPathNormalizer normalizer;

        public PatternMatcher(IUrlPathNormalizer normalizer)
        {
            this.normalizer = CheckValue(normalizer, nameof(normalizer));
        }

        /// <inheritdoc/>
        public bool TryMatch(
            CompiledPattern compiledPattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match,
            [NotNullWhen(false)] out string? why)
        {
            CheckValue(compiledPattern, nameof(compiledPattern));
            CheckValue(path, nameof(path));

            var normalizedPath = normalizer.Normalize(path);
            Match regexMatch;
            try
            {
                regexMatch = compiledPattern.Regex.Match(normalizedPath);
            }
            catch (RegexMatchTimeoutException)
            {
                why = $"The attempt to match path '{path}' with pattern '{compiledPattern.Pattern}' timed out.";
                match = null;
                return false;
            }

            if (!regexMatch.Success)
            {
                why = $"The path '{path}' is not a match for pattern '{compiledPattern.Pattern}'";
                match = null;
                return false;
            }

            var variables = GetVariables(regexMatch, compiledPattern.VariableNames);
            var pathSuffix = GetPathSuffix(regexMatch, normalizedPath);

            why = null;
            match = new PatternMatchResult(pathSuffix, variables);
            return true;
        }

        private static IReadOnlyDictionary<string, string> GetVariables(Match match, ISet<string> variableNames)
        {
            var groups = match.Groups;
            Dictionary<string, string>? result = null;

            foreach (var variableName in variableNames)
            {
                if (groups.TryGetValue(variableName, out var group) && group.Success)
                {
                    if (result is null)
                        result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    result.Add(variableName, group.Value);
                }
            }

            return result is null
                ? ImmutableDictionary<string, string>.Empty
                : (IReadOnlyDictionary<string, string>)result;
        }

        private static string GetPathSuffix(Match regexMatch, string path)
           => path.Substring(regexMatch.Length);
    }
}
