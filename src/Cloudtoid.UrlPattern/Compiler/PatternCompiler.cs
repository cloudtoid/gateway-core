namespace Cloudtoid.UrlPattern
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
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
            [NotNullWhen(false)] out IReadOnlyList<PatternCompilerError>? errors)
        {
            CheckValue(pattern, nameof(pattern));

            var errorsSink = new PatternCompilerErrorsSink();

            // 1- Parse
            if (!parser.TryParse(pattern, errorsSink, out var parsedPattern))
            {
                compiledPattern = null;
                errors = errorsSink.Errors;
                return false;
            }

            // 2- Validate
            if (!validator.Validate(parsedPattern, errorsSink))
            {
                compiledPattern = null;
                errors = errorsSink.Errors;
                return false;
            }

            // 3- Build regex
            var regex = new PatternRegexBuilder().Build(parsedPattern);

            // 4- Get variable names
            var variables = new VariableNamesExtractor().Extract(parsedPattern);

            compiledPattern = new CompiledPattern(
                pattern,
                parsedPattern,
                regex,
                variables);

            errors = null;
            return true;
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
