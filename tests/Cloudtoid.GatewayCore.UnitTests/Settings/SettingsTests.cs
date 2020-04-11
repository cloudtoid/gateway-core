namespace Cloudtoid.GatewayCore.UnitTests
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Cloudtoid.GatewayCore;
    using Cloudtoid.GatewayCore.Expression;
    using Cloudtoid.GatewayCore.Host;
    using Cloudtoid.GatewayCore.Settings;
    using Cloudtoid.GatewayCore.Trace;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public sealed class SettingsTests
    {
        [TestMethod]
        public void New_FullyPopulatedOptions_AllValuesAreReadCorrectly()
        {
            var settings = GetSettings(@"Settings\OptionsFull.json");
            settings.System.RouteCacheMaxCount.Should().Be(1024);

            var context = GetProxyContext(settings);
            var routeSettings = context.Route.Settings;
            routeSettings.Route.Should().Be("/api/");

            routeSettings.Proxy!.GetCorrelationIdHeader(context).Should().Be("x-request-id");

            var request = routeSettings.Proxy.UpstreamRequest;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version30);

            var requestHeaders = request.Headers;
            requestHeaders.TryGetProxyName(context, out var proxyName).Should().BeTrue();
            proxyName.Should().Be("some-proxy-name");
            requestHeaders.GetDefaultHost(context).Should().Be("this-machine-name");
            requestHeaders.AllowHeadersWithEmptyValue.Should().BeTrue();
            requestHeaders.AllowHeadersWithUnderscoreInName.Should().BeTrue();
            requestHeaders.IgnoreAllDownstreamHeaders.Should().BeTrue();
            requestHeaders.IgnoreCallId.Should().BeTrue();
            requestHeaders.IgnoreForwardedFor.Should().BeTrue();
            requestHeaders.IgnoreForwardedProtocol.Should().BeTrue();
            requestHeaders.IgnoreForwardedHost.Should().BeTrue();
            requestHeaders.IgnoreCorrelationId.Should().BeTrue();
            requestHeaders.IncludeExternalAddress.Should().BeTrue();
            requestHeaders.Overrides.Select(h => (h.Name, Values: h.GetValues(context)))
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (Name: "x-extra-1", Values: new[] { "value1_1", "value1_2" }),
                        (Name: "x-extra-2", Values: new[] { "value2_1", "value2_2" })
                    });

            var requestSender = request.Sender;
            requestSender.HttpClientName.Should().Be("api-route-http-client-name");
            requestSender.GetTimeout(context).TotalMilliseconds.Should().Be(5200);
            requestSender.AllowAutoRedirect.Should().BeTrue();
            requestSender.UseCookies.Should().BeTrue();

            var response = routeSettings.Proxy.DownstreamResponse;
            var responseHeaders = response.Headers;
            responseHeaders.AllowHeadersWithEmptyValue.Should().BeTrue();
            responseHeaders.AllowHeadersWithUnderscoreInName.Should().BeTrue();
            responseHeaders.Overrides.Select(h => (h.Name, Values: h.GetValues(context)))
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (Name: "x-extra-1", Values: new[] { "value1_1", "value1_2" }),
                        (Name: "x-extra-2", Values: new[] { "value2_1", "value2_2" })
                    });
        }

        [TestMethod]
        public void New_AllOptionsThatAllowExpressions_AllValuesAreEvaluatedCorrectly()
        {
            var context = GetProxyContext(@"Settings\OptionsWithExpressions.json");
            var settings = context.Route.Settings;

            var expressionValue = Environment.MachineName;
            settings.Proxy!.GetCorrelationIdHeader(context).Should().Be("CorrelationIdHeader:" + expressionValue);

            var request = settings.Proxy.UpstreamRequest;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version11);

            var requestHeaders = request.Headers;
            requestHeaders.TryGetProxyName(context, out var proxyName).Should().BeTrue();
            proxyName.Should().Be("ProxyName:" + expressionValue);
            requestHeaders.GetDefaultHost(context).Should().Be("DefaultHost:" + expressionValue);
            requestHeaders.Overrides.Select(h => (h.Name, Values: h.GetValues(context)))
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (Name: "x-extra-1", Values: new[] { "x-extra-1:v1:" + expressionValue, "x-extra-1:v2:" + expressionValue }),
                        (Name: "x-extra-2", Values: new[] { "x-extra-2:v1:" + expressionValue, "x-extra-2:v2:" + expressionValue })
                    });

            var requestSender = request.Sender;
            requestSender.GetTimeout(context).TotalMilliseconds.Should().Be(5200);

            var response = settings.Proxy.DownstreamResponse;
            var responseHeaders = response.Headers;
            responseHeaders.Overrides.Select(h => (h.Name, Values: h.GetValues(context)))
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (Name: "x-extra-1", Values: new[] { "x-extra-1:v1:" + expressionValue, "x-extra-1:v2:" + expressionValue }),
                        (Name: "x-extra-2", Values: new[] { "x-extra-2:v1:" + expressionValue, "x-extra-2:v2:" + expressionValue })
                    });
        }

        [TestMethod]
        public void New_EmptyOptions_AllValuesSetToDefault()
        {
            var settings = GetSettings(@"Settings\OptionsEmpty.json");
            settings.System.RouteCacheMaxCount.Should().Be(100000);

            var context = GetProxyContext(settings);
            var routeSettings = context.Route.Settings;

            var request = routeSettings.Proxy!.UpstreamRequest;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version20);

            var requestHeaders = request.Headers;
            requestHeaders.TryGetProxyName(context, out var proxyName).Should().BeTrue();
            proxyName.Should().Be("gwcore");
            requestHeaders.GetDefaultHost(context).Should().Be(Environment.MachineName);
            requestHeaders.AllowHeadersWithEmptyValue.Should().BeFalse();
            requestHeaders.AllowHeadersWithUnderscoreInName.Should().BeFalse();
            requestHeaders.IgnoreAllDownstreamHeaders.Should().BeFalse();
            requestHeaders.IgnoreCallId.Should().BeFalse();
            requestHeaders.IgnoreForwardedFor.Should().BeFalse();
            requestHeaders.IgnoreForwardedProtocol.Should().BeFalse();
            requestHeaders.IgnoreForwardedHost.Should().BeFalse();
            requestHeaders.IgnoreCorrelationId.Should().BeFalse();
            requestHeaders.IncludeExternalAddress.Should().BeFalse();
            requestHeaders.Overrides.Should().BeEmpty();

            var requestSender = request.Sender;
            requestSender.HttpClientName.Should().Be(GuidProvider.StringValue);
            requestSender.GetTimeout(context).TotalMilliseconds.Should().Be(240000);
            requestSender.AllowAutoRedirect.Should().BeFalse();
            requestSender.UseCookies.Should().BeFalse();

            var response = routeSettings.Proxy.DownstreamResponse;
            var responseHeaders = response.Headers;
            responseHeaders.AllowHeadersWithEmptyValue.Should().BeFalse();
            responseHeaders.AllowHeadersWithUnderscoreInName.Should().BeFalse();
            responseHeaders.Overrides.Should().BeEmpty();
        }

        [TestMethod]
        public void New_OptionsFileModified_FileIsReloaded()
        {
            try
            {
                File.Copy(@"Settings\OptionsOld.json", @"Settings\OptionsReload.json", true);

                var config = new ConfigurationBuilder()
                    .AddJsonFile(@"Settings\OptionsReload.json", optional: false, reloadOnChange: true)
                    .Build();

                var services = new ServiceCollection()
                    .AddTest()
                    .Configure<GatewayOptions>(config);

                var serviceProvider = services.BuildServiceProvider();
                var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
                var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<GatewayOptions>>();

                var httpContext = new DefaultHttpContext();
                var settings = settingsProvider.CurrentValue.Routes.First();

                var context = new ProxyContext(
                    Substitute.For<IHostProvider>(),
                    Substitute.For<ITraceIdProvider>(),
                    httpContext,
                    new Route(settings, string.Empty, ImmutableDictionary<string, string>.Empty));

                using (var changeEvent = new AutoResetEvent(false))
                {
                    void Reset(object o)
                    {
                        changeEvent.Set();
                        monitor.OnChange(Reset);
                    }

                    monitor.OnChange(Reset);

                    settings.Proxy!.UpstreamRequest.Sender.GetTimeout(context).TotalMilliseconds.Should().Be(5000);

                    File.Copy(@"Settings\OptionsNew.json", @"Settings\OptionsReload.json", true);
                    changeEvent.WaitOne(2000);

                    settings = settingsProvider.CurrentValue.Routes.First();
                    context = new ProxyContext(
                        Substitute.For<IHostProvider>(),
                        Substitute.For<ITraceIdProvider>(),
                        httpContext,
                        new Route(settings, string.Empty, ImmutableDictionary<string, string>.Empty));

                    settings.Proxy!.UpstreamRequest.Sender.GetTimeout(context).TotalMilliseconds.Should().Be(2000);

                    File.Copy(@"Settings\OptionsOld.json", @"Settings\OptionsReload.json", true);
                    changeEvent.WaitOne(2000);
                    settings = settingsProvider.CurrentValue.Routes.First();
                    context = new ProxyContext(
                        Substitute.For<IHostProvider>(),
                        Substitute.For<ITraceIdProvider>(),
                        httpContext,
                        new Route(settings, string.Empty, ImmutableDictionary<string, string>.Empty));

                    settings.Proxy!.UpstreamRequest.Sender.GetTimeout(context).TotalMilliseconds.Should().Be(5000);
                }
            }
            finally
            {
                File.Delete(@"Settings\OptionsReload.json");
            }
        }

        [TestMethod]
        public void New_BadRouteCacheMaxCount_CorrectError()
        {
            var options = new GatewayOptions();
            options.System.RouteCacheMaxCount = 10;
            var settings = CreateSettingsAndCheckLogs(
                options,
                "RouteCacheMaxCount must be set to at least 1000. Using the default value which is 100000 instead.");

            settings.System.RouteCacheMaxCount.Should().Be(100000);
        }

        [TestMethod]
        public void New_NoRoutes_CorrectError()
        {
            var options = new GatewayOptions();
            CreateSettingsAndCheckLogs(options, "No routes are specified");
        }

        [TestMethod]
        public void New_EmptyRoute_CorrectError()
        {
            var options = new GatewayOptions();
            options.Routes.Add(string.Empty, new GatewayOptions.RouteOptions());
            CreateSettingsAndCheckLogs(options, "A route cannot be an empty string");
        }

        [TestMethod]
        public void New_NullProxy_CorrectError()
        {
            const string route = "/a/b/c/";
            var options = new GatewayOptions();
            options.Routes.Add(route, new GatewayOptions.RouteOptions
            {
                Proxy = null
            });

            var settings = CreateSettings(options);
            settings.Routes.Should().HaveCount(1);
            settings.Routes[0].Proxy.Should().BeNull();
        }

        [TestMethod]
        public void New_NullProxyTo_CorrectError()
        {
            var options = new GatewayOptions();
            options.Routes.Add("/a/b/c/", new GatewayOptions.RouteOptions
            {
                Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                {
                    To = null
                }
            });

            CreateSettingsAndCheckLogs(options, "The 'To' cannot be empty or skipped.");
        }

        [TestMethod]
        public void New_EmptyProxyTo_CorrectError()
        {
            var options = new GatewayOptions();
            options.Routes.Add("/a/b/c/", new GatewayOptions.RouteOptions
            {
                Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                {
                    To = "    "
                }
            });

            CreateSettingsAndCheckLogs(options, "The 'To' cannot be empty or skipped.");
        }

        [TestMethod]
        public void New_FailsToCompileRoutePattern_Fail()
        {
            var options = new GatewayOptions();
            options.Routes.Add($"/category/:id/product/:id", new GatewayOptions.RouteOptions
            {
                Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                {
                    To = "/somevalue"
                }
            });

            CreateSettingsAndCheckLogs(options, "The variable name 'id' has already been used. Variable names must be unique.");
        }

        [TestMethod]
        public void New_CollidesWithSystemVariable_Fail()
        {
            var options = new GatewayOptions();
            options.Routes.Add($"/:{SystemVariableNames.Host}/", new GatewayOptions.RouteOptions
            {
                Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                {
                    To = "/somevalue"
                }
            });

            CreateSettingsAndCheckLogs(options, "The variable name 'host' collides with a system variable with the same name.");
        }

        [TestMethod]
        public void New_InvalidHeaderName_CorrectError()
        {
            var options = new GatewayOptions();
            options.Routes.Add("/a/b/c/", new GatewayOptions.RouteOptions
            {
                Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                {
                    To = "/e/f/g/",
                    UpstreamRequest = new GatewayOptions.RouteOptions.ProxyOptions.UpstreamRequestOptions
                    {
                        Headers = new GatewayOptions.RouteOptions.ProxyOptions.UpstreamRequestOptions.HeadersOptions
                        {
                            Overrides = new System.Collections.Generic.Dictionary<string, string[]>()
                            {
                                [" bad-header\\"] = new[] { "value" }
                            }
                        }
                    }
                }
            });

            CreateSettingsAndCheckLogs(options, "The ' bad-header\\' is not a valid HTTP header name. It will be ignored.");
        }

        private static GatewaySettings GetSettings(string jsonFile)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(jsonFile, optional: false)
                .Build();

            var services = new ServiceCollection()
                .AddTest()
                .Configure<GatewayOptions>(config);

            return services
                .BuildServiceProvider()
                .GetRequiredService<ISettingsProvider>()
                .CurrentValue;
        }

        private static ProxyContext GetProxyContext(GatewaySettings settings)
        {
            var httpContext = new DefaultHttpContext();
            var routeSettings = settings.Routes.First();

            return new ProxyContext(
                Substitute.For<IHostProvider>(),
                Substitute.For<ITraceIdProvider>(),
                httpContext,
                new Route(routeSettings, string.Empty, ImmutableDictionary<string, string>.Empty));
        }

        private static ProxyContext GetProxyContext(string jsonFile)
        {
            var settings = GetSettings(jsonFile);
            return GetProxyContext(settings);
        }

        private static GatewaySettings CreateSettings(GatewayOptions options)
        {
            var services = new ServiceCollection().AddTest().AddTestOptions(options);
            var serviceProvider = services.BuildServiceProvider();
            var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
            return settingsProvider.CurrentValue;
        }

        private static GatewaySettings CreateSettingsAndCheckLogs(GatewayOptions options, params string[] messages)
        {
            var services = new ServiceCollection().AddTest().AddTestOptions(options);
            var serviceProvider = services.BuildServiceProvider();
            var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();

            var logger = (Logger<SettingsCreator>)serviceProvider.GetRequiredService<ILogger<SettingsCreator>>();
            foreach (var message in messages)
                logger.Logs.Any(l => l.ContainsOrdinalIgnoreCase(message)).Should().BeTrue();

            return settingsProvider.CurrentValue;
        }
    }
}
