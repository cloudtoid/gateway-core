namespace Cloudtoid.UrlPattern.UnitTests
{
    using System.Linq;
    using Cloudtoid.UrlPattern;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PatternMatcherTests
    {
        private readonly IUrlPathNormalizer normalizer;
        private readonly IPatternCompiler compiler;
        private readonly IPatternMatcher matcher;

        public PatternMatcherTests()
        {
            var services = new ServiceCollection().AddUrlPattern();
            var serviceProvider = services.BuildServiceProvider();
            normalizer = serviceProvider.GetRequiredService<IUrlPathNormalizer>();
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

            ShouldMatch("/product/:id", "product/1234", ("id", "1234"));
            ShouldMatch("/product(/:id)", "product/1234", ("id", "1234"));
            ShouldMatch("/product(/:id)", "product");

            ShouldMatch(
                pattern: "/category/:category/product/:product",
                route: "category/bike/product/1234",
                ("category", "bike"),
                ("product", "1234"));

            ShouldMatch(
                pattern: "/category/:category(/product/:product)",
                route: "category/bike/product/1234",
                ("category", "bike"),
                ("product", "1234"));

            ShouldMatch(
                pattern: "/category/:category(/product/:product)",
                route: "category/bike/",
                ("category", "bike"));

            ShouldMatch(
               pattern: "/category/*(/product/:product)",
               route: "category/bike/product/1234",
               ("product", "1234"));

            ShouldMatch(
               pattern: "/category/*(/product/:product)",
               route: "category/bike");

            ShouldNotMatch(
               pattern: "/category/*(/product/:product)",
               route: "/bike(/product/:product)");

            ShouldMatch(
               pattern: "/category/*(/product/:product)",
               route: "category/bike");

            ShouldNotMatch(
              pattern: "/category/*(/product/:product)",
              route: "/bike(/product/:product)");

            ShouldMatch(
               pattern: @"/category/\\*(/product/:product)",
               route: "category/*/product/1234",
               ("product", "1234"));

            ShouldMatch(
               pattern: @"/category/\\*(/product/:product)",
               route: "category/*/");

            ShouldMatch(
               pattern: @"/category/\\*/product/:product",
               route: "category/*/product/1234",
               ("product", "1234"));

            ShouldMatch(
               pattern: @"/category/\\:product",
               route: "category/:product/");

            ShouldMatch(
               pattern: @"/category/\\(:product\\)",
               route: "category/(1234)/",
               ("product", "1234"));

            ShouldNotMatch(
               pattern: @"/category/\\(:product\\)",
               route: "category/1234/");

            ShouldMatch(
               pattern: @"/category/\(:product\)",
               route: @"category/\1234\/",
               ("product", "1234"));

            ShouldMatch(
               pattern: @"/category/*",
               route: "category/1234/");

            ShouldMatch(
               pattern: @"/category/*",
               route: "category/");

            ShouldMatch(
               pattern: @"/category/*/product",
               route: "category/bike/product");
        }

        private void ShouldMatch(string pattern, string route, params (string Name, string Value)[] variables)
        {
            var compiledPattern = Compile(pattern);
            matcher.TryMatch(compiledPattern, normalizer.Normalize(route), out var matched).Should().BeTrue();
            matched.Should().NotBeNull();
            matched!.Variables.Select(v => (v.Key, v.Value)).Should().BeEquivalentTo(variables);
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
