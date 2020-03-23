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
                SegmentlNode.Instance,
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
                    SegmentlNode.Instance,
                    new MatchNode("api"),
                    SegmentlNode.Instance,
                    new MatchNode("v"),
                    new VariableNode("version"),
                    SegmentlNode.Instance),
                o => o.RespectingRuntimeTypes());

            var exp = SegmentlNode.Instance
                + new MatchNode("api")
                + SegmentlNode.Instance
                + new MatchNode("v")
                + new VariableNode("version")
                + SegmentlNode.Instance;

            pattern.Should().BeEquivalentTo(
                 exp,
                 o => o.RespectingRuntimeTypes());

            error.Should().BeNull();
        }

        // /api/v:version/
        // /api/v:version/product/:id/
        // /api/v:version/product/:id
        // /api(/v:version)/product(/:id)
        // /api(/v:version)/product/(:id)
        // /(api/v:version/)product/
    }
}
