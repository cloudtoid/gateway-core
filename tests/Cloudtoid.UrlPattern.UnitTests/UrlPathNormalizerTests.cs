namespace Cloudtoid.UrlPattern.UnitTests
{
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UrlPathNormalizerTests
    {
        private readonly IUrlPathNormalizer normalizer;

        public UrlPathNormalizerTests()
        {
            var services = new ServiceCollection().AddUrlPattern();
            var serviceProvider = services.BuildServiceProvider();
            normalizer = serviceProvider.GetRequiredService<IUrlPathNormalizer>();
        }

        [TestMethod]
        public void NormalizeTests()
        {
            Normalize(string.Empty, "/");
            Normalize("/", "/");
            Normalize("/ ", "/");
            Normalize(" /", "/");
            Normalize(" / ", "/");
            Normalize("/product/", "/product/");
            Normalize(" /product/ ", "/product/");
            Normalize("/product", "/product/");
            Normalize("/product/1234/", "/product/1234/");
        }

        private void Normalize(string route, string expected)
        {
            normalizer.Normalize(route).Should().Be(expected);
        }
    }
}
