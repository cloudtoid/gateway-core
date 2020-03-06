namespace Cloudtoid.Foid.UnitTests
{
    using System.IO;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ConfigTests
    {
        [TestMethod]
        public void New_FullyPopulatedProxyConfig_AllValuesAreReadCorrectly()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile(@"Config\ProxyConfig1.json", optional: true, reloadOnChange: true)
                .Build();

            var config = new Config(builder);
            config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(5000);
        }

        [TestMethod]
        public void New_NotFullyPopulatedProxyConfig_AllValuesHaveADefault()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile(@"Config\ConfigEmpty.json", optional: true, reloadOnChange: true)
                .Build();

            var config = new Config(builder);
            config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(240000);
        }

        [TestMethod]
        public void New_ConfigFileModified_FileIsReloaded()
        {
            try
            {
                File.Copy(@"Config\ProxyConfig1.json", @"Config\ProxyConfigReload.json", true);

                var builder = new ConfigurationBuilder()
                    .AddJsonFile(@"Config\ProxyConfigReload.json", optional: true, reloadOnChange: true)
                    .Build();

                var config = new Config(builder);
                config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(5000);

                File.Copy(@"Config\ProxyConfig2.json", @"Config\ProxyConfigReload.json", true);
                config.ChangeEvent.WaitOne(2000);

                config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(2000);

                File.Copy(@"Config\ProxyConfig1.json", @"Config\ProxyConfigReload.json", true);
                config.ChangeEvent.WaitOne(2000);

                config.Value.Upstream.Request.TotalTimeout.TotalMilliseconds.Should().Be(5000);
            }
            finally
            {
                File.Delete(@"Config\ProxyConfigReload.json");
            }
        }
    }
}
