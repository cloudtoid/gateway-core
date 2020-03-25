namespace Cloudtoid.Foid.Routes.Pattern
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using static Contract;

    internal sealed class PatternCompiler : IPatternCompiler
    {
        private readonly IPatternParser parser;
        private readonly IPatternValidator validator;

        public PatternCompiler(
            IPatternParser parser,
            IPatternValidator validator)
        {
            this.parser = CheckValue(parser, nameof(parser));
            this.validator = CheckValue(validator, nameof(validator));
        }

        public bool TryCompile(
            string pattern,
            [NotNullWhen(true)] out CompiledPattern? compiledPattern,
            [NotNullWhen(false)] out string? errors)
        {
            CheckValue(pattern, nameof(pattern));

            // 1- Parse
            if (!parser.TryParse(pattern, out var parsedPattern, out var parserErrors))
            {
                compiledPattern = null;
                errors = ConvertToString(pattern, parserErrors);
                return false;
            }

            // 2- Validate
            if (!validator.Validate(parsedPattern, out errors))
            {
                compiledPattern = null;
                return false;
            }

            // 3- Build regex
            var regex = new PatternRegexBuilder().Build(parsedPattern);

            // 4- Get variable names
            var variables = new VariableNamesExtractor().Extract(parsedPattern);

            compiledPattern = new CompiledPattern(regex, variables);
            return true;
        }

        private string ConvertToString(string route, IReadOnlyList<PatternParserError> errors)
        {
            var builder = new StringBuilder($"Route '{route}' failed to parse with the following error(s):");
            builder.AppendLine();
            for (int i = 0; i < errors.Count; i++)
            {
                var error = errors[i];
                builder.Append(error.Message);

                if (error.Location != null)
                    builder.AppendFormatInvariant(" (location:{0})", error.Location);
            }

            return builder.ToString();
        }

        private sealed class VariableNamesExtractor : PatternNodeVisitor
        {
            private readonly ISet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public ISet<string> Extract(PatternNode node)
            {
                Visit(node);
                return names;
            }

            protected internal override void VisitVariable(VariableNode node) => names.Add(node.Name);
        }
    }
}
