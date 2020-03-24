namespace Cloudtoid.Foid.Routes.Pattern
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using static Contract;

    internal sealed class CompiledPattern
    {
        internal CompiledPattern(Regex regex, ISet<string> variables)
        {
            Regex = CheckValue(regex, nameof(regex));
            Variables = CheckValue(variables, nameof(variables));
        }

        public Regex Regex { get; }

        public ISet<string> Variables { get; }
    }
}
