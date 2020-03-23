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
            pattern.Should().BeEquivalentTo(new MatchNode("a"));
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenValidLongMatchRoute_ReturnsMatchNodeAndNoError()
        {
            var parser = new PatternParser();
            parser.TryParse("valid-value", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(new MatchNode("valid-value"));
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenSegmentChar_ReturnsSegmentNode()
        {
            var parser = new PatternParser();
            parser.TryParse("/", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(SegmentlNode.Instance);
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenWildcardChar_ReturnsWildcardNode()
        {
            var parser = new PatternParser();
            parser.TryParse("*", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(WildcardNode.Instance);
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenOptionalMatch_ReturnsOptionalWithMatchNode()
        {
            var parser = new PatternParser();
            parser.TryParse("(value)", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(new OptionalNode(new MatchNode("value")));
            error.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WhenOptionalWild_ReturnsOptionalWithWildNode()
        {
            var parser = new PatternParser();
            parser.TryParse("(*)", out var pattern, out var error).Should().BeTrue();
            pattern.Should().BeEquivalentTo(new OptionalNode(WildcardNode.Instance));
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
            pattern.Should().BeEquivalentTo(new VariableNode("variable"));
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

        // /api/v:version/
        // /api/v:version/product/:id/
        // /api/v:version/product/:id
        // /api(/v:version)/product(/:id)
        // /api(/v:version)/product/(:id)
        // /(api/v:version/)product/
        // )
        // (
    }
}
