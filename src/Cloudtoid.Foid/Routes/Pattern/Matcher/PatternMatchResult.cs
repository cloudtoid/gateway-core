namespace Cloudtoid.Foid.Routes.Pattern
{
    using System.Collections.Generic;
    using static Contract;

    internal sealed class PatternMatchResult
    {
        internal PatternMatchResult(IReadOnlyDictionary<string, string> variables)
        {
            Variables = CheckValue(variables, nameof(variables));
        }

        internal IReadOnlyDictionary<string, string> Variables { get; set; }
    }
}
