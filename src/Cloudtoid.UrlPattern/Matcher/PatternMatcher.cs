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
        /// <inheritdoc/>
        public bool TryMatch(
            CompiledPattern compiledPattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match,
            [NotNullWhen(false)] out string? why)
        {
            CheckValue(compiledPattern, nameof(compiledPattern));
            CheckValue(path, nameof(path));

            Match regexMatch;
            try
            {
                regexMatch = compiledPattern.Regex.Match(path);
            }
            catch (RegexMatchTimeoutException)
            {
                why = $"The attempt to match path '{path}' with pattern '{compiledPattern.Pattern}' timed out.";
                match = null;
                return false;
            }

            if (!regexMatch.Success)
            {
                why = $"The path '{path}' with pattern '{compiledPattern.Pattern}'";
                match = null;
                return false;
            }

            var variables = GetVariables(regexMatch, compiledPattern.VariableNames);
            var pathSuffix = GetPathSuffix(regexMatch, path);

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

            if (result is null)
                return ImmutableDictionary<string, string>.Empty;

            return result;
        }

        private static string GetPathSuffix(Match regexMatch, string path)
        {
            var len = regexMatch.Length;

            if (len == path.Length)
                return string.Empty;

            if (len == 0)
                return path;

            var start = len;
            var end = path.Length - 1;

            while (start <= end && path[start] == '/')
                start++;

            while (start < end && path[end] == '/')
                end--;

            return path.Substring(start, end - start + 1);
        }
    }
}
