namespace Cloudtoid.Foid.Routes.Pattern
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
            string route,
            [NotNullWhen(true)] out PatternMatchResult? match)
        {
            CheckValue(pattern, nameof(pattern));
            CheckValue(route, nameof(route));

            logger.LogDebug("Matching path '{0}' with pattern '{1}'.", route, pattern.Pattern);

            match = Match(pattern, route);

            if (match is null)
            {
                logger.LogDebug("Path '{0}' is not a match for pattern '{1}'.", route, pattern.Pattern);
                return false;
            }

            logger.LogDebug("Matched '{0}' path with '{1}' pattern.", route, pattern.Pattern);
            return true;
        }

        private PatternMatchResult? Match(
            CompiledPattern pattern,
            string route)
        {
            Match regexMatch;
            try
            {
                regexMatch = pattern.Regex.Match(route);
            }
            catch (RegexMatchTimeoutException tex)
            {
                logger.LogError(tex, "The attempt to match path '{0}' with pattern '{1}' timed out.", route, pattern.Pattern);
                return null;
            }

            if (!regexMatch.Success)
                return null;

            var variables = GetVariables(regexMatch, pattern.VariableNames);
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
