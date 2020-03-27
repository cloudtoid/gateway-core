namespace Cloudtoid.UrlPattern
{
    using System.Collections.Generic;
    using static Contract;

    public sealed class PatternMatchResult
    {
        internal PatternMatchResult(IReadOnlyDictionary<string, string> variables)
        {
            Variables = CheckValue(variables, nameof(variables));
        }

        public IReadOnlyDictionary<string, string> Variables { get; }
    }
}
