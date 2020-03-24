namespace Cloudtoid.Foid.Routes.Pattern
{
    using System;
    using System.Collections.Generic;
    using static Contract;

    internal sealed class PatternCompiler : IPatternCompiler
    {
        public CompiledPattern Compile(PatternNode pattern)
        {
            CheckValue(pattern, nameof(pattern));

            var regex = new PatternRegexBuilder().Build(pattern);
            var variables = new VariableNamesReader().ReadNames(pattern);
            return new CompiledPattern(regex, variables);
        }

        private sealed class VariableNamesReader : PatternNodeVisitor
        {
            private readonly ISet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public ISet<string> ReadNames(PatternNode node)
            {
                Visit(node);
                return names;
            }

            protected internal override void VisitVariable(VariableNode node) => names.Add(node.Name);
        }
    }
}
