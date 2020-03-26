namespace Cloudtoid.Foid.UnitTests
{
    using Cloudtoid.Foid.Routes;
    using Cloudtoid.Foid.Routes.Pattern;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PatternMatcherTests
    {
        private readonly IRouteNormalizer normalizer;
        private readonly IPatternCompiler compiler;
        private readonly IPatternMatcher matcher;

        public PatternMatcherTests()
        {
            var services = new ServiceCollection().AddTest();
            var serviceProvider = services.BuildServiceProvider();
            normalizer = serviceProvider.GetRequiredService<IRouteNormalizer>();
            compiler = serviceProvider.GetRequiredService<IPatternCompiler>();
            matcher = serviceProvider.GetRequiredService<IPatternMatcher>();
        }

        [TestMethod]
        public void TryMatchTests()
        {
            ShouldNotMatch("/segment", "/product");

            ShouldMatch("/product", "/product");
            ShouldMatch("/product/", "/product");
            ShouldMatch(" /product/ ", "/product");
            ShouldMatch("/product", "product");
            ShouldMatch("/product/*/", "product/1234");
            ShouldMatch("/product/1*/", "product/1234");
            ShouldNotMatch("/product/13*/", "product/1234");
            ShouldMatch("/product/(1*/)", "product/");
            ShouldMatch("/product/(1*/)", "product/1234/");
            ShouldMatch("/product/(1*/)", "product/1234");
            ShouldMatch("(/product)/(1*/)", "product/1234");
            ShouldMatch("(/product)/(1*/)", "/1234");
            ShouldMatch("(/product)/(1*/)", "1234");
            ShouldMatch("(/product)/(1*/)", "1234/");

            // TODO: this should match ParseBuildAndMatch("/product/(1*/)", "product/1234/");
        }

        private void ShouldMatch(string pattern, string route)
        {
            var compiledPattern = Compile(pattern);
            matcher.TryMatch(compiledPattern, normalizer.Normalize(route), out var matched).Should().BeTrue();
            matched.Should().NotBeNull();
        }

        private void ShouldNotMatch(string pattern, string route)
        {
            var compiledPattern = Compile(pattern);
            matcher.TryMatch(compiledPattern, normalizer.Normalize(route), out var matched).Should().BeFalse();
            matched.Should().BeNull();
        }

        private CompiledPattern Compile(string pattern)
        {
            compiler.TryCompile(pattern, out var compiledPattern, out var errors).Should().BeTrue();
            errors.Should().BeNull();
            compiledPattern.Should().NotBeNull();
            return compiledPattern!;
        }
    }
}
