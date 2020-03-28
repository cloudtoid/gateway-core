namespace Cloudtoid.UrlPattern
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using static Contract;

    public sealed class CompiledPattern
    {
        internal CompiledPattern(
            string pattern,
            Regex regex,
            ISet<string> variableNames)
        {
            Pattern = CheckValue(pattern, nameof(pattern));
            Regex = CheckValue(regex, nameof(regex));
            VariableNames = CheckValue(variableNames, nameof(variableNames));
        }

        public string Pattern { get; }

        public Regex Regex { get; }

        public ISet<string> VariableNames { get; }
    }
}
