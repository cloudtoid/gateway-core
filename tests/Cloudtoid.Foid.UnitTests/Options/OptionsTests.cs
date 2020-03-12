namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using static FoidOptions.ProxyOptions;

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
                .AddTest()
                .Configure<FoidOptions>(config);

            var options = services
                .BuildServiceProvider()
                .GetRequiredService<OptionsProvider>();

            var context = new DefaultHttpContext();
            options.Proxy.GetCorrelationIdHeader(context).Should().Be("x-request-id");

            var request = options.Proxy.Upstream.Request;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version30);
            request.GetTimeout(context).TotalMilliseconds.Should().Be(5200);

            var requestHeaders = request.Headers;
            requestHeaders.TryGetProxyName(context, out var proxyName).Should().BeTrue();
            proxyName.Should().Be("some-proxy-name");
            requestHeaders.GetDefaultHost(context).Should().Be("this-machine-name");
            requestHeaders.AllowHeadersWithEmptyValue.Should().BeTrue();
            requestHeaders.AllowHeadersWithUnderscoreInName.Should().BeTrue();
            requestHeaders.IgnoreAllDownstreamRequestHeaders.Should().BeTrue();
            requestHeaders.IgnoreCallId.Should().BeTrue();
            requestHeaders.IgnoreClientAddress.Should().BeTrue();
            requestHeaders.IgnoreClientProtocol.Should().BeTrue();
            requestHeaders.IgnoreCorrelationId.Should().BeTrue();
            requestHeaders.IncludeExternalAddress.Should().BeTrue();
            requestHeaders.Headers.Select(
                h => new ExtraHeader
                {
                    Name = h.Name,
                    Values = h.GetValues(context).ToArray()
                })
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        new ExtraHeader
                        {
                            Name = "x-xtra-1",
                            Values = new[] { "value1_1", "value1_2" }
                        },
                        new ExtraHeader
                        {
                            Name = "x-xtra-2",
                            Values = new[] { "value2_1", "value2_2" }
                        },
                    });

            var response = options.Proxy.Downstream.Response;
            var responseHeaders = response.Headers;
            responseHeaders.AllowHeadersWithEmptyValue.Should().BeTrue();
            responseHeaders.AllowHeadersWithUnderscoreInName.Should().BeTrue();
            responseHeaders.Headers.Select(
                h => new ExtraHeader
                {
                    Name = h.Name,
                    Values = h.GetValues(context).ToArray()
                })
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        new ExtraHeader
                        {
                            Name = "x-xtra-1",
                            Values = new[] { "value1_1", "value1_2" }
                        },
                        new ExtraHeader
                        {
                            Name = "x-xtra-2",
                            Values = new[] { "value2_1", "value2_2" }
                        },
                    });
        }

        [TestMethod]
        public void New_AllOptionsThatAllowExpressions_AllValuesAreEvaluatedCorrectly()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(@"Options\OptionsWithExpressions.json", optional: false)
                .Build();

            var services = new ServiceCollection()
                .AddTest()
                .Configure<FoidOptions>(config);

            var options = services
                .BuildServiceProvider()
                .GetRequiredService<OptionsProvider>();

            var expressionValue = Environment.MachineName;
            var context = new DefaultHttpContext();
            options.Proxy.GetCorrelationIdHeader(context).Should().Be("CorrelationIdHeader:" + expressionValue);

            var request = options.Proxy.Upstream.Request;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version11);
            request.GetTimeout(context).TotalMilliseconds.Should().Be(5200);

            var requestHeaders = request.Headers;
            requestHeaders.TryGetProxyName(context, out var proxyName).Should().BeTrue();
            proxyName.Should().Be("ProxyName:" + expressionValue);
            requestHeaders.GetDefaultHost(context).Should().Be("DefaultHost:" + expressionValue);
            requestHeaders.Headers.Select(
                h => new ExtraHeader
                {
                    Name = h.Name,
                    Values = h.GetValues(context).ToArray()
                })
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        new ExtraHeader
                        {
                            Name = "x-xtra-1",
                            Values = new[] { "x-xtra-1:v1:" + expressionValue, "x-xtra-1:v2:" + expressionValue }
                        },
                        new ExtraHeader
                        {
                            Name = "x-xtra-2",
                            Values = new[] { "x-xtra-2:v1:" + expressionValue, "x-xtra-2:v2:" + expressionValue }
                        },
                    });

            var response = options.Proxy.Downstream.Response;
            var responseHeaders = response.Headers;
            responseHeaders.Headers.Select(
                h => new ExtraHeader
                {
                    Name = h.Name,
                    Values = h.GetValues(context).ToArray()
                })
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        new ExtraHeader
                        {
                            Name = "x-xtra-1",
                            Values = new[] { "x-xtra-1:v1:" + expressionValue, "x-xtra-1:v2:" + expressionValue }
                        },
                        new ExtraHeader
                        {
                            Name = "x-xtra-2",
                            Values = new[] { "x-xtra-2:v1:" + expressionValue, "x-xtra-2:v2:" + expressionValue }
                        },
                    });
        }

        [TestMethod]
        public void New_OptionsEmpty_AllValuesSetToDefault()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(@"Options\OptionsEmpty.json", optional: false)
                .Build();

            var services = new ServiceCollection()
                .AddTest()
                .Configure<FoidOptions>(config);

            var options = services
                .BuildServiceProvider()
                .GetRequiredService<OptionsProvider>();

            var context = new DefaultHttpContext();

            var request = options.Proxy.Upstream.Request;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version20);
            request.GetTimeout(context).TotalMilliseconds.Should().Be(240000);

            var requestHeaders = request.Headers;
            requestHeaders.TryGetProxyName(context, out var proxyName).Should().BeTrue();
            proxyName.Should().Be("foid");
            requestHeaders.GetDefaultHost(context).Should().Be(Environment.MachineName);
            requestHeaders.AllowHeadersWithEmptyValue.Should().BeFalse();
            requestHeaders.AllowHeadersWithUnderscoreInName.Should().BeFalse();
            requestHeaders.IgnoreAllDownstreamRequestHeaders.Should().BeFalse();
            requestHeaders.IgnoreCallId.Should().BeFalse();
            requestHeaders.IgnoreClientAddress.Should().BeFalse();
            requestHeaders.IgnoreClientProtocol.Should().BeFalse();
            requestHeaders.IgnoreCorrelationId.Should().BeFalse();
            requestHeaders.IncludeExternalAddress.Should().BeFalse();
            requestHeaders.Headers.Should().BeEmpty();

            var response = options.Proxy.Downstream.Response;
            var responseHeaders = response.Headers;
            responseHeaders.AllowHeadersWithEmptyValue.Should().BeFalse();
            responseHeaders.AllowHeadersWithUnderscoreInName.Should().BeFalse();
            responseHeaders.Headers.Should().BeEmpty();
        }

        [TestMethod]
        public void New_OptionsgFileModified_FileIsReloaded()
        {
            try
            {
                File.Copy(@"Options\Options1.json", @"Options\OptionsReload.json", true);

                var config = new ConfigurationBuilder()
                    .AddJsonFile(@"Options\OptionsReload.json", optional: false, reloadOnChange: true)
                    .Build();

                var services = new ServiceCollection()
                    .AddTest()
                    .Configure<FoidOptions>(config);

                var serviceProvider = services.BuildServiceProvider();
                var options = serviceProvider.GetRequiredService<OptionsProvider>();
                var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<FoidOptions>>();

                var context = new DefaultHttpContext();

                var changeEvent = new AutoResetEvent(false);

                void Reset(object o)
                {
                    changeEvent.Set();
                    monitor.OnChange(Reset);
                }

                monitor.OnChange(Reset);

                options.Proxy.Upstream.Request.GetTimeout(context).TotalMilliseconds.Should().Be(5000);

                File.Copy(@"Options\Options2.json", @"Options\OptionsReload.json", true);
                changeEvent.WaitOne(2000);

                options.Proxy.Upstream.Request.GetTimeout(context).TotalMilliseconds.Should().Be(2000);

                File.Copy(@"Options\Options1.json", @"Options\OptionsReload.json", true);
                changeEvent.WaitOne(2000);

                options.Proxy.Upstream.Request.GetTimeout(context).TotalMilliseconds.Should().Be(5000);
            }
            finally
            {
                File.Delete(@"Options\OptionsReload.json");
            }
        }
    }
}
