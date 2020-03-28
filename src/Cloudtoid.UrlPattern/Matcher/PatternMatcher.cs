namespace Cloudtoid.UrlPattern
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class PatternMatcher : IPatternMatcher
    {
        private readonly ILogger<PatternMatcher> logger;

        public PatternMatcher(ILogger<PatternMatcher> logger)
        {
            this.logger = CheckValue(logger, nameof(logger));
        }

        public bool TryMatch(
            CompiledPattern pattern,
            string path,
            [NotNullWhen(true)] out PatternMatchResult? match)
        {
            CheckValue(pattern, nameof(pattern));
            CheckValue(path, nameof(path));

            logger.LogDebug("Matching path '{0}' with pattern '{1}'.", path, pattern.Pattern);

            match = Match(pattern, path);

            if (match is null)
            {
                logger.LogDebug("Path '{0}' is not a match for pattern '{1}'.", path, pattern.Pattern);
                return false;
            }

            logger.LogDebug("Matched '{0}' path with '{1}' pattern.", path, pattern.Pattern);
            return true;
        }

        private PatternMatchResult? Match(
            CompiledPattern compiledPattern,
            string path)
        {
            Match regexMatch;
            try
            {
                regexMatch = compiledPattern.Regex.Match(path);
            }
            catch (RegexMatchTimeoutException tex)
            {
                logger.LogError(tex, "The attempt to match path '{0}' with pattern '{1}' timed out.", path, compiledPattern.Pattern);
                return null;
            }

            if (!regexMatch.Success)
                return null;

            var variables = GetVariables(regexMatch, compiledPattern.VariableNames);
            return new PatternMatchResult(variables);
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
    }
}
