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

            ShouldMatch("/product/:id", "product/1234", variables: ("id", "1234"));
            ShouldMatch("/product(/:id)", "product/1234", variables: ("id", "1234"));
            ShouldMatch("/product(/:id)", "product");

            ShouldMatch(
                pattern: "/category/:category/product/:product",
                route: "category/bike/product/1234",
                string.Empty,
                ("category", "bike"),
                ("product", "1234"));

            ShouldMatch(
                pattern: "/category/:category(/product/:product)",
                route: "category/bike/product/1234",
                string.Empty,
                ("category", "bike"),
                ("product", "1234"));

            ShouldMatch(
                pattern: "/category/:category(/product/:product)",
                route: "category/bike/",
                variables: ("category", "bike"));

            ShouldMatch(
               pattern: "/category/*(/product/:product)",
               route: "category/bike/product/1234",
               variables: ("product", "1234"));

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
               variables: ("product", "1234"));

            ShouldMatch(
               pattern: @"/category/\\*(/product/:product)",
               route: "category/*/");

            ShouldMatch(
               pattern: @"/category/\\*/product/:product",
               route: "category/*/product/1234",
               variables: ("product", "1234"));

            ShouldMatch(
               pattern: @"/category/\\:product",
               route: "category/:product/");

            ShouldMatch(
               pattern: @"/category/\\(:product\\)",
               route: "category/(1234)/",
               variables: ("product", "1234"));

            ShouldNotMatch(
               pattern: @"/category/\\(:product\\)",
               route: "category/1234/");

            ShouldMatch(
               pattern: @"/category/\(:product\)",
               route: @"category/\1234\/",
               variables: ("product", "1234"));

            ShouldMatch(
               pattern: @"/category/*",
               route: "category/1234/");

            ShouldMatch(
               pattern: @"/category/*",
               route: "category/");

            ShouldNotMatch(
               pattern: @"exact: /category/",
               route: "category/test");

            ShouldMatch(
               pattern: @"/category/",
               route: "category/bike/",
               pathSuffix: "bike");

            ShouldMatch(
               pattern: @"/category/",
               route: "category/bike/product/1234",
               pathSuffix: "bike/product/1234");

            ShouldMatch(
               pattern: @"/catego",
               route: "category/bike/product/1234",
               pathSuffix: "ry/bike/product/1234");

            ShouldMatch(
               pattern: @"/category/*/product",
               route: "category/bike/product");

            ShouldMatch(
               pattern: @"/",
               route: "/////category/bike/product/////",
               pathSuffix: "category/bike/product");

            ShouldMatch(
               pattern: @"/",
               route: "///////");

            ShouldMatch(
               pattern: @"regex: \/category\/(?<category>.+)\/product",
               route: "category/bike/product",
               variables: ("category", "bike"));

            ShouldMatch(
               pattern: @"regex: \/category\/(?<category>.+)\/product",
               route: "category/bike/product/123/test/",
               pathSuffix: "123/test",
               variables: ("category", "bike"));
        }

        private void ShouldMatch(
            string pattern,
            string route,
            string? pathSuffix = null,
            params (string Name, string Value)[] variables)
        {
            var compiledPattern = Compile(pattern);
            matcher.TryMatch(compiledPattern, normalizer.Normalize(route), out var matched).Should().BeTrue();
            matched.Should().NotBeNull();
            matched!.PathSuffix.Should().BeEquivalentTo(pathSuffix ?? string.Empty);
            matched.Variables.Select(v => (v.Key, v.Value)).Should().BeEquivalentTo(variables);
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
