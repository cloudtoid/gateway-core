namespace Cloudtoid.Foid.Routes.Pattern
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;

    internal sealed class PatternCompiler : IPatternCompiler
    {
        public CompiledPattern Compile(PatternNode pattern) => throw new NotImplementedException();

        private sealed class RegexBuilder : PatternNodeVisitor
        {
            private static readonly string SegmentStart = Regex.Escape(@"/");
            private static readonly string Wildcard = $"[^{SegmentStart}]+";  // [^\/]+
            private readonly StringBuilder builder = new StringBuilder($@"\A(?:{SegmentStart})?");

            internal void Compile(PatternNode pattern)
            {
                Visit(pattern);
            }

            protected internal override void VisitMatch(MatchNode node)
            {
                builder.Append(Regex.Escape(node.Value));
            }

            protected internal override void VisitVariable(VariableNode node)
            {
                // TODO: Instead of [^/], should we be exact about what characters can be included?

                // - Generates a regex capture with the name of the variable:  (?<variable>[^\/]+)
                // - Variable name does not need to be escaped or validated. The PatternParser ensures that it only contains 'a-zA-Z0-9_'
                //   and the first character is not a number.
                builder.Append($"(?<{node.Name}>[^{SegmentStart}]+)");
            }

            protected internal override void VisitSegmentStart(SegmentStartNode node)
            {
                builder.Append(SegmentStart);
            }

            protected internal override void VisitWildcard(WildcardNode node)
            {
                builder.Append(Wildcard);
            }

            protected internal override void VisitOptional(OptionalNode node)
            {
                // regex: (?:node)?

                builder.Append("(?:");
                base.VisitOptional(node);
                builder.Append(")?");
            }
        }
    }
}
