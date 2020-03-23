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
                SegmentNode.Instance,
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
                    SegmentNode.Instance,
                    new MatchNode("api"),
                    SegmentNode.Instance,
                    new MatchNode("v"),
                    new VariableNode("version"),
                    SegmentNode.Instance),
                o => o.RespectingRuntimeTypes());

            pattern.Should().BeEquivalentTo(
                SegmentNode.Instance
                + new MatchNode("api")
                + SegmentNode.Instance
                + new MatchNode("v")
                + new VariableNode("version")
                + SegmentNode.Instance,
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
                    SegmentNode.Instance,
                    new MatchNode("api"),
                    SegmentNode.Instance,
                    new MatchNode("v"),
                    new VariableNode("version")),
                o => o.RespectingRuntimeTypes());

            pattern.Should().BeEquivalentTo(
                SegmentNode.Instance
                + new MatchNode("api")
                + SegmentNode.Instance
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
                SegmentNode.Instance
                + new MatchNode("api")
                + SegmentNode.Instance
                + new MatchNode("v")
                + new VariableNode("version")
                + SegmentNode.Instance
                + new MatchNode("product")
                + SegmentNode.Instance
                + new VariableNode("id")
                + SegmentNode.Instance,
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenMultipleVariablesExtended_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api/v:version/product/:id", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                SegmentNode.Instance
                + new MatchNode("api")
                + SegmentNode.Instance
                + new MatchNode("v")
                + new VariableNode("version")
                + SegmentNode.Instance
                + new MatchNode("product")
                + SegmentNode.Instance
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
                SegmentNode.Instance
                + new MatchNode("api")
                + new OptionalNode((SegmentNode.Instance + new MatchNode("v1.0"))!)
                + SegmentNode.Instance
                + new MatchNode("product")
                + SegmentNode.Instance
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
                SegmentNode.Instance
                + new MatchNode("api")
                + new OptionalNode((SegmentNode.Instance + new MatchNode("v") + new VariableNode("version"))!)
                + SegmentNode.Instance
                + new MatchNode("product")
                + new OptionalNode((SegmentNode.Instance + new VariableNode("id"))!),
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenMultipleOptionlSegmentsWithVariablesExtended_Success()
        {
            var parser = new PatternParser();
            parser.TryParse("/api(/v:version)/product/(:id)", out var pattern, out var error).Should().BeTrue();

            pattern.Should().BeEquivalentTo(
                SegmentNode.Instance
                + new MatchNode("api")
                + new OptionalNode((SegmentNode.Instance + new MatchNode("v") + new VariableNode("version"))!)
                + SegmentNode.Instance
                + new MatchNode("product")
                + SegmentNode.Instance
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
                SegmentNode.Instance
                + new OptionalNode((new MatchNode("api") + SegmentNode.Instance + new MatchNode("v") + new VariableNode("version") + SegmentNode.Instance)!)
                + new MatchNode("product")
                + SegmentNode.Instance,
                o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        // /api/v:version/
        // /api/v:version/product/:id/
        // /api/v:version/product/:id
        // /api(/v1.0)/product/:id
        // /api(/v:version)/product(/:id)
        // /api(/v:version)/product/(:id)
        // /(api/v:version/)product/
    }
}
