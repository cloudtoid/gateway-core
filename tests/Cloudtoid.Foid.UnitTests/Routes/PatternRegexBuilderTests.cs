namespace Cloudtoid.Foid.UnitTests
{
    using System.Text.RegularExpressions;
    using Cloudtoid.Foid.Routes.Pattern;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PatternRegexBuilderTests
    {
        [TestMethod]
        public void Build_OneSegmentNotMatch_NotMatch()
        {
            ParseBuildAndMatchFail("/segment", "/product");

            ParseBuildAndMatch("/product", "/product");
            ParseBuildAndMatch("/product/", "/product");
            ParseBuildAndMatch(" /product/ ", "/product");
            ParseBuildAndMatch("/product", "product");
            ParseBuildAndMatch("/product/*/", "product/1234");
            ParseBuildAndMatch("/product/1*/", "product/1234");
            ParseBuildAndMatchFail("/product/13*/", "product/1234");
            ParseBuildAndMatch("/product/(1*/)", "product/");
            ParseBuildAndMatch("/product/(1*/)", "product/1234/");

            // TODO: this should match ParseBuildAndMatch("/product/(1*/)", "product/1234/");
        }

        private static void ParseBuildAndMatchFail(string pattern, string match)
        {
            var regex = ParseAndBuild(pattern);
            regex.IsMatch(match).Should().BeFalse();
        }

        private static void ParseBuildAndMatch(string pattern, string match)
        {
            var regex = ParseAndBuild(pattern);
            regex.IsMatch(match).Should().BeTrue();
        }

        private static Regex ParseAndBuild(string pattern)
        {
            var parser = new PatternParser();
            parser.TryParse(pattern, out var parsedPattern, out var error).Should().BeTrue();
            error.Should().BeNull();
            var builder = new PatternRegexBuilder();
            return builder.Build(parsedPattern!);
        }
    }
}
