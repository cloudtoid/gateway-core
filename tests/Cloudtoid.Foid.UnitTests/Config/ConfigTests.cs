namespace Cloudtoid.Foid.UnitTests
{
    using System.Collections.Generic;
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
                .AddJsonFile(@"Config\ProxyConfigFull.json")
                .Build();

            var config = new Config(builder);

            var request = config.Value.Upstream.Request;
            request.TotalTimeout.TotalMilliseconds.Should().Be(5200);

            var requestHeaders = config.Value.Upstream.Request.Headers;
            requestHeaders.ProxyName.Should().Be("some-proxy-name");
            requestHeaders.DefaultHost.Should().Be("this-machine-name");
            requestHeaders.AllowHeadersWithEmptyValue.Should().BeTrue();
            requestHeaders.AllowHeadersWithUnderscoreInName.Should().BeTrue();
            requestHeaders.IgnoreCallId.Should().BeTrue();
            requestHeaders.IgnoreClientAddress.Should().BeTrue();
            requestHeaders.IgnoreClientProtocol.Should().BeTrue();
            requestHeaders.IgnoreRequestId.Should().BeTrue();
            requestHeaders.IncludeExternalAddress.Should().BeTrue();
            requestHeaders.ExtraHeaders.Should().BeEquivalentTo(
                new (string Key, IEnumerable<string> Values)[]
                {
                    ("x-xtra-1", new[] { "value1_1", "value1_2" }),
                    ("x-xtra-2", new[] { "value2_1", "value2_2" }),
                });
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
