namespace Cloudtoid.Foid.UnitTests
{
    using System.IO;
    using System.Threading;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using static Cloudtoid.Foid.FoidOptions.ProxyOptions.UpstreamOptions.RequestOptions.HeadersOptions;

    [TestClass]
    public sealed class OptionsTests
    {
        [TestMethod]
        public void New_FullyPopulatedOptions_AllValuesAreReadCorrectly()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(@"Options\OptionsFull.json", optional: false)
                .Build();

            var services = new ServiceCollection()
                .AddOptions()
                .Configure<FoidOptions>(config);

            var options = services
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<FoidOptions>>();

            var request = options.CurrentValue.Proxy.Upstream.Request;
            request.TimeoutInMilliseconds.Should().Be(5200);

            var requestHeaders = request.Headers;
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
                new[]
                {
                    new ExtraHeader
                    {
                        Key = "x-xtra-1",
                        Values = new[] { "value1_1", "value1_2" }
                    },
                    new ExtraHeader
                    {
                        Key = "x-xtra-2",
                        Values = new[] { "value2_1", "value2_2" }
                    },
                });
        }

        [TestMethod]
        public void New_NotFullyPopulatedOptions_AllValuesHaveADefault()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(@"Options\ConfigEmpty.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection()
                .AddOptions()
                .Configure<FoidOptions>(config);

            var options = services
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<FoidOptions>>();

            options.CurrentValue.Proxy.Upstream.Request.TimeoutInMilliseconds.Should().Be(240000);
        }

        [TestMethod]
        public void New_ConfigFileModified_FileIsReloaded()
        {
            try
            {
                File.Copy(@"Options\Options1.json", @"Options\OptionsReload.json", true);

                var config = new ConfigurationBuilder()
                    .AddJsonFile(@"Options\OptionsReload.json", optional: false, reloadOnChange: true)
                    .Build();

                var services = new ServiceCollection()
                    .AddOptions()
                    .Configure<FoidOptions>(config);

                var options = services
                    .BuildServiceProvider()
                    .GetRequiredService<IOptionsMonitor<FoidOptions>>();

                var changeEvent = new AutoResetEvent(false);

                void Reset(object o)
                {
                    changeEvent.Set();
                    options.OnChange(Reset);
                }

                options.OnChange(Reset);

                options.CurrentValue.Proxy.Upstream.Request.TimeoutInMilliseconds.Should().Be(5000);

                File.Copy(@"Options\Options2.json", @"Options\OptionsReload.json", true);
                changeEvent.WaitOne(2000);

                options.CurrentValue.Proxy.Upstream.Request.TimeoutInMilliseconds.Should().Be(2000);

                File.Copy(@"Options\Options1.json", @"Options\OptionsReload.json", true);
                changeEvent.WaitOne(2000);

                options.CurrentValue.Proxy.Upstream.Request.TimeoutInMilliseconds.Should().Be(5000);
            }
            finally
            {
                File.Delete(@"Options\OptionsReload.json");
            }
        }
    }
}
