namespace Cloudtoid.Foid.UnitTests
{
    using System.IO;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ProxyConfigTests
    {
        [TestMethod]
        public void InstantiateProvider_FullyPopulatedProxyConfig_AllValuesAreReadCorrectly()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("Configs\\ProxyConfig1.json", optional: true, reloadOnChange: true)
                .Build();

            var config = new Config(builder);
            config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(5000);
        }

        [TestMethod]
        public void InstantiateProvider_NotFullyPopulatedProxyConfig_AllValuesHaveADefault()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("Configs\\ProxyConfigEmpty.json", optional: true, reloadOnChange: true)
                .Build();

            var config = new Config(builder);
            config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(240000);
        }

        [TestMethod]
        public void InstantiateProvider_ConfigFileModified_FileIsReloaded()
        {
            try
            {
                File.Copy("Configs\\ProxyConfig1.json", "Configs\\ProxyConfigReload.json", true);

                var builder = new ConfigurationBuilder()
                    .AddJsonFile("Configs\\ProxyConfigReload.json", optional: true, reloadOnChange: true)
                    .Build();

                var config = new Config(builder);
                config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(5000);

                File.Copy("Configs\\ProxyConfig2.json", "Configs\\ProxyConfigReload.json", true);
                config.ChangeEvent.WaitOne(2000);

                config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(2000);

                File.Copy("Configs\\ProxyConfig1.json", "Configs\\ProxyConfigReload.json", true);
                config.ChangeEvent.WaitOne(2000);

                config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(5000);
            }
            finally
            {
                File.Delete("Configs\\ProxyConfigReload.json");
            }
        }
    }
}
