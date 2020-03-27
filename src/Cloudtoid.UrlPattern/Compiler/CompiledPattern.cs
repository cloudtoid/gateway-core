namespace Cloudtoid.UrlPattern
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using static Contract;

    public sealed class CompiledPattern
    {
        internal CompiledPattern(
            string pattern,
            PatternNode parsedPattern,
            Regex regex,
            ISet<string> variableNames)
        {
            Pattern = CheckValue(pattern, nameof(pattern));
            ParsedPattern = CheckValue(parsedPattern, nameof(parsedPattern));
            Regex = CheckValue(regex, nameof(regex));
            VariableNames = CheckValue(variableNames, nameof(variableNames));
        }

        public string Pattern { get; }

        internal PatternNode ParsedPattern { get; }

        public Regex Regex { get; }

        public ISet<string> VariableNames { get; }
    }
}
