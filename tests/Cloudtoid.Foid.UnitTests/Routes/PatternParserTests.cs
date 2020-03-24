namespace Cloudtoid.Foid.UnitTests
{
    using Cloudtoid.Foid.Routes.Pattern;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class PatternParserTests
    {
        [TestMethod]
        public void TryParse_WhenEmptyRoute_ReturnsEmptyMatchAndNoError()
        {
            var parser = new PatternParser();
            parser.TryParse(string.Empty, out var pattern, out var error).Should().BeTrue();
            pattern.Should().Be(MatchNode.Empty);
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenValidSingleCharMatchRoute_ReturnsMatchNodeAndNoError()
        {
            var parser = new PatternParser();
            parser.TryParse("a", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(
                new MatchNode("a"),
                o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenValidLongMatchRoute_ReturnsMatchNodeAndNoError()
        {
            var parser = new PatternParser();
            parser.TryParse("valid-value", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(
                new MatchNode("valid-value"),
                o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenSegmentChar_ReturnsSegmentNode()
        {
            var parser = new PatternParser();
            parser.TryParse("/", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance,
                o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenWildcardChar_ReturnsWildcardNode()
        {
            var parser = new PatternParser();
            parser.TryParse("*", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(
                WildcardNode.Instance,
                o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenOptionalMatch_ReturnsOptionalWithMatchNode()
        {
            var parser = new PatternParser();
            parser.TryParse("(value)", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(
                new OptionalNode(new MatchNode("value")),
                o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenOptionalWild_ReturnsOptionalWithWildNode()
        {
            var parser = new PatternParser();
            parser.TryParse("(*)", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(
                new OptionalNode(WildcardNode.Instance),
                o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenEmptyOptional_ReturnsNullPatternAndError()
        {
            var parser = new PatternParser();
            parser.TryParse("()", out var pattern, out var error).Should().BeFalse();
            pattern.Should().BeNull();
            error.Should().Contain("empty or invalid");
        }

        [TestMethod]
        public void TryParse_WhenOptionalStartOnly_ReturnsNullPatternAndError()
        {
            var parser = new PatternParser();
            parser.TryParse("(", out var pattern, out var error).Should().BeFalse();
            pattern.Should().BeNull();
            error.Should().Contain("There is a missing ')'");
        }

        [TestMethod]
        public void TryParse_WhenOptionalEndOnly_ReturnsNullPatternAndError()
        {
            var parser = new PatternParser();
            parser.TryParse(")", out var pattern, out var error).Should().BeFalse();
            pattern.Should().BeNull();
            error.Should().Contain("There is an unexpected ')'");
        }

        [TestMethod]
        public void TryParse_WhenSimpleVariable_ReturnsVariableNode()
        {
            var parser = new PatternParser();
            parser.TryParse(":variable", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(
                new VariableNode("variable"),
                o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenEmptyVariable_ReturnsNullPatternAndError()
        {
            var parser = new PatternParser();
            parser.TryParse(":-----", out var pattern, out var error).Should().BeFalse();
            pattern.Should().BeNull();
            error.Should().Contain("invalid name");
        }

        [TestMethod]
        public void TryParse_WhenSegmentIsVariable_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api/v:version/", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                new SequenceNode(
                    SegmentStartNode.Instance,
                    new MatchNode("api"),
                    SegmentStartNode.Instance,
                    new MatchNode("v"),
                    new VariableNode("version"),
                    SegmentStartNode.Instance),
                o => o.RespectingRuntimeTypes());

            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance
                + new MatchNode("api")
                + SegmentStartNode.Instance
                + new MatchNode("v")
                + new VariableNode("version")
                + SegmentStartNode.Instance,
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenSegmentIsVariableExtended_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api/v:version", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                new SequenceNode(
                    SegmentStartNode.Instance,
                    new MatchNode("api"),
                    SegmentStartNode.Instance,
                    new MatchNode("v"),
                    new VariableNode("version")),
                o => o.RespectingRuntimeTypes());

            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance
                + new MatchNode("api")
                + SegmentStartNode.Instance
                + new MatchNode("v")
                + new VariableNode("version"),
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenMultipleVariables_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api/v:version/product/:id/", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance
                + new MatchNode("api")
                + SegmentStartNode.Instance
                + new MatchNode("v")
                + new VariableNode("version")
                + SegmentStartNode.Instance
                + new MatchNode("product")
                + SegmentStartNode.Instance
                + new VariableNode("id")
                + SegmentStartNode.Instance,
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenMultipleVariablesExtended_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api/v:version/product/:id", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance
                + new MatchNode("api")
                + SegmentStartNode.Instance
                + new MatchNode("v")
                + new VariableNode("version")
                + SegmentStartNode.Instance
                + new MatchNode("product")
                + SegmentStartNode.Instance
                + new VariableNode("id"),
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenOptionalSegment_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api(/v1.0)/product/:id", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance
                + new MatchNode("api")
                + new OptionalNode((SegmentStartNode.Instance + new MatchNode("v1.0"))!)
                + SegmentStartNode.Instance
                + new MatchNode("product")
                + SegmentStartNode.Instance
                + new VariableNode("id"),
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenMultipleOptionlSegmentsWithVariables_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api(/v:version)/product(/:id)", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance
                + new MatchNode("api")
                + new OptionalNode((SegmentStartNode.Instance + new MatchNode("v") + new VariableNode("version"))!)
                + SegmentStartNode.Instance
                + new MatchNode("product")
                + new OptionalNode((SegmentStartNode.Instance + new VariableNode("id"))!),
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenMultipleOptionlSegmentsWithVariablesExtended_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api(/v:version)/product/(:id)", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance
                + new MatchNode("api")
                + new OptionalNode((SegmentStartNode.Instance + new MatchNode("v") + new VariableNode("version"))!)
                + SegmentStartNode.Instance
                + new MatchNode("product")
                + SegmentStartNode.Instance
                + new OptionalNode(new VariableNode("id")),
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenComplexOptional_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/(api/v:version/)product/", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                SegmentStartNode.Instance
                + new OptionalNode((new MatchNode("api") + SegmentStartNode.Instance + new MatchNode("v") + new VariableNode("version") + SegmentStartNode.Instance)!)
                + new MatchNode("product")
                + SegmentStartNode.Instance,
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenUsingEscapeCharButNotFollowedByEscapableChar_EscapeCharsShouldBeMatched()
        {
            var values = new[]
            {
                @"\",
                @"\\",
                @"\\a",
                @"\\value",
                @"value\",
                @"value\\",
                @"value\\a",
                @"some\\value",
                @"\\a\",
                @"\\a\\",
                @"\\a\\b",
                @"\\some\\value"
            };

            foreach (var value in values)
                ExpectMatch(value);

            values = new[]
            {
                @":variable\",
                @":variable\\",
                @":variable\\a",
                @":variable\\value",
                @":variable-value\",
                @":variable-value\\",
                @":variable-value\\a",
                @":variable-some\\value",
                @":variable\\a\",
                @":variable\\a\\",
                @":variable\\a\\b",
                @":variable\\some\\value",
                @"\:variable",
                @"\\a:variable",
                @"\\value:variable",
                @"-value\:variable",
                @"-value\\a:variable",
                @"-some\\value:variable",
                @"\\a\:variable",
                @"\\a\\b:variable",
                @"\\some\\value:variable"
            };

            foreach (var value in values)
                ExpectVariableAndMatch(value);

            values = new[]
            {
                @"\:variable",
                @"\\a:variable",
                @"\\value:variable",
                @"-value\:variable",
                @"-value\\a:variable",
                @"-some\\value:variable",
                @"\\a\:variable",
                @"\\a\\b:variable",
                @"\\some\\value:variable"
            };

            foreach (var value in values)
                ExpectEndVariableAndMatch(value);
        }

        private static void ExpectMatch(string value)
        {
            new PatternParser().TryParse(value, out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(new MatchNode(value), o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        private static void ExpectVariableAndMatch(string value)
        {
            new PatternParser().TryParse(value, out var pattern, out var error).Should().BeTrue();
            pattern.Should()
                .BeEquivalentTo(
                    new VariableNode("variable") + new MatchNode(value.ReplaceOrdinal(":variable", string.Empty)),
                    o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        private static void ExpectEndVariableAndMatch(string value)
        {
            new PatternParser().TryParse(value, out var pattern, out var error).Should().BeTrue();
            pattern.Should()
                    .BeEquivalentTo(
                        new MatchNode(value.ReplaceOrdinal(":variable", string.Empty)) + new VariableNode("variable"),
                        o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenEscapedAtBegining_SpecialCharIsEscaped()
        {
            var values = new[]
            {
                @"\\:var",
                @"\\*",
                @"\\(",
                @"\\)",
                @"\\*value",
                @"\\(value",
                @"\\)value",
            };

            foreach (var value in values)
                ExpectEscapedAtBeginingMatch(value);
        }

        private static void ExpectEscapedAtBeginingMatch(string value)
        {
            new PatternParser().TryParse(value, out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(new MatchNode(value.Substring(2)), o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenEscapedInMiddle_SpecialCharIsEscaped()
        {
            var values = new[]
            {
                @"a\\:var",
                @"a\\*",
                @"a\\(",
                @"a\\)",
                @"a\\*value",
                @"a\\(value",
                @"a\\)value",
            };

            foreach (var value in values)
                ExpectEscapedInMiddleMatch(value);
        }

        private static void ExpectEscapedInMiddleMatch(string value)
        {
            new PatternParser().TryParse(value, out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(new MatchNode("a") + new MatchNode(value.Substring(3)), o => o.RespectingRuntimeTypes());
            error.Should().BeNull();
        }

        // /api/v:version/
        // /api/v:version/product/:id/
        // /api/v:version/product/:id
        // /api(/v1.0)/product/:id
        // /api(/v:version)/product(/:id)
        // /api(/v:version)/product/(:id)
        // /(api/v:version/)product/
        // \
        // \\
        // \\a
        // \\value
        // value\
        // value\\
        // value\\a
        // some\\value
        // \\a\
        // \\a\\
        // \\a\\b
        // \\some\\value
    }
}
