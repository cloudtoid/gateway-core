namespace Cloudtoid.GatewayCore.UnitTests
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid.GatewayCore;
    using Cloudtoid.GatewayCore.Expression;
    using Cloudtoid.GatewayCore.Settings;
    using Cloudtoid.GatewayCore.Trace;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public sealed class SettingsTests
    {
        private IServiceProvider? serviceProvider;

        [TestMethod]
        public void New_FullyPopulatedOptions_AllValuesAreReadCorrectly()
        {
            var settings = GetSettings(@"Settings/OptionsFull.json");
            settings.System.RouteCacheMaxCount.Should().Be(1024);

            var context = GetProxyContext(settings);
            var routeSettings = context.Route.Settings;
            routeSettings.Route.Should().Be("/api/");

            context.ProxyName.Should().Be("some-proxy-name");
            routeSettings.Proxy!.GetCorrelationIdHeader(context).Should().Be("x-request-id");

            var request = routeSettings.Proxy.UpstreamRequest;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version30);

            var requestHeaders = request.Headers;
            requestHeaders.AllowHeadersWithEmptyValue.Should().BeTrue();
            requestHeaders.AllowHeadersWithUnderscoreInName.Should().BeTrue();
            requestHeaders.IgnoreAllDownstreamHeaders.Should().BeTrue();
            requestHeaders.IgnoreVia.Should().BeTrue();
            requestHeaders.IgnoreCorrelationId.Should().BeTrue();
            requestHeaders.IgnoreCallId.Should().BeTrue();
            requestHeaders.IgnoreForwarded.Should().BeTrue();
            requestHeaders.UseXForwarded.Should().BeTrue();
            requestHeaders.IncludeExternalAddress.Should().BeTrue();
            requestHeaders.Overrides.Values.Select(h => (h.Name, Values: h.GetValues(context)))
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
            requestSender.ConnectTimeout.TotalMilliseconds.Should().Be(1000);
            requestSender.Expect100ContinueTimeout.TotalMilliseconds.Should().Be(2000);
            requestSender.PooledConnectionIdleTimeout.TotalMilliseconds.Should().Be(3000);
            requestSender.PooledConnectionLifetime.TotalMilliseconds.Should().Be(4000);
            requestSender.ResponseDrainTimeout.TotalMilliseconds.Should().Be(5000);
            requestSender.MaxAutomaticRedirections.Should().Be(10);
            requestSender.MaxConnectionsPerServer.Should().Be(20);
            requestSender.MaxResponseDrainSizeInBytes.Should().Be(12800);
            requestSender.MaxResponseHeadersLengthInKilobytes.Should().Be(128);
            requestSender.AllowAutoRedirect.Should().BeTrue();
            requestSender.UseCookies.Should().BeTrue();

            var response = routeSettings.Proxy.DownstreamResponse;
            var responseHeaders = response.Headers;
            responseHeaders.IgnoreAllUpstreamHeaders.Should().BeTrue();
            responseHeaders.IgnoreVia.Should().BeTrue();
            responseHeaders.IncludeCorrelationId.Should().BeTrue();
            responseHeaders.IncludeCallId.Should().BeTrue();
            responseHeaders.Cookies.Values
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        new CookieSettings(
                            "*",
                            true,
                            false,
                            Microsoft.Net.Http.Headers.SameSiteMode.Lax,
                            "example.com"),
                        new CookieSettings(
                            "sessionId",
                            false,
                            true,
                            Microsoft.Net.Http.Headers.SameSiteMode.Strict,
                            "sample.com"),
                        new CookieSettings(
                            "userCookie",
                            null,
                            null,
                            Microsoft.Net.Http.Headers.SameSiteMode.None,
                            "user.com"),
                        new CookieSettings(
                            "testCookie",
                            null,
                            null,
                            Microsoft.Net.Http.Headers.SameSiteMode.Unspecified,
                            "test.com")
                    });
            responseHeaders.Overrides.Values.Select(h => (h.Name, Values: h.GetValues(context)))
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
            var context = GetProxyContext(@"Settings/OptionsWithExpressions.json");
            var settings = context.Route.Settings;

            var expressionValue = Environment.MachineName;
            context.ProxyName.Should().Be("ProxyName:" + expressionValue);
            settings.Proxy!.GetCorrelationIdHeader(context).Should().Be("CorrelationIdHeader:" + expressionValue);

            var request = settings.Proxy.UpstreamRequest;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version11);

            var requestHeaders = request.Headers;
            requestHeaders.Overrides.Values.Select(h => (h.Name, Values: h.GetValues(context)))
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
            responseHeaders.Overrides.Values.Select(h => (h.Name, Values: h.GetValues(context)))
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
            var settings = GetSettings(@"Settings/OptionsEmpty.json");
            settings.System.RouteCacheMaxCount.Should().Be(100000);

            var context = GetProxyContext(settings);
            context.ProxyName.Should().Be("gwcore");
            var routeSettings = context.Route.Settings;

            var request = routeSettings.Proxy!.UpstreamRequest;
            request.GetHttpVersion(context).Should().Be(HttpVersion.Version20);

            var requestHeaders = request.Headers;
            requestHeaders.AllowHeadersWithEmptyValue.Should().BeFalse();
            requestHeaders.AllowHeadersWithUnderscoreInName.Should().BeFalse();
            requestHeaders.IgnoreAllDownstreamHeaders.Should().BeFalse();
            requestHeaders.IgnoreVia.Should().BeFalse();
            requestHeaders.IgnoreCorrelationId.Should().BeFalse();
            requestHeaders.IgnoreCallId.Should().BeFalse();
            requestHeaders.IgnoreForwarded.Should().BeFalse();
            requestHeaders.UseXForwarded.Should().BeFalse();
            requestHeaders.IncludeExternalAddress.Should().BeFalse();
            requestHeaders.Overrides.Should().BeEmpty();

            var requestSender = request.Sender;
            requestSender.HttpClientName.Should().Be(GuidProvider.StringValue);
            requestSender.GetTimeout(context).TotalMilliseconds.Should().Be(240000);
            requestSender.ConnectTimeout.Should().Be(Timeout.InfiniteTimeSpan);
            requestSender.Expect100ContinueTimeout.TotalMilliseconds.Should().Be(1000);
            requestSender.PooledConnectionIdleTimeout.Should().Be(TimeSpan.FromMinutes(2));
            requestSender.PooledConnectionLifetime.Should().Be(Timeout.InfiniteTimeSpan);
            requestSender.ResponseDrainTimeout.TotalMilliseconds.Should().Be(2000);
            requestSender.MaxAutomaticRedirections.Should().Be(50);
            requestSender.MaxConnectionsPerServer.Should().Be(int.MaxValue);
            requestSender.MaxResponseDrainSizeInBytes.Should().Be(1024 * 1024);
            requestSender.MaxResponseHeadersLengthInKilobytes.Should().Be(64);
            requestSender.AllowAutoRedirect.Should().BeFalse();
            requestSender.UseCookies.Should().BeFalse();

            var response = routeSettings.Proxy.DownstreamResponse;
            var responseHeaders = response.Headers;
            responseHeaders.AllowHeadersWithEmptyValue.Should().BeFalse();
            responseHeaders.AllowHeadersWithUnderscoreInName.Should().BeFalse();
            responseHeaders.IgnoreAllUpstreamHeaders.Should().BeFalse();
            responseHeaders.IgnoreVia.Should().BeFalse();
            responseHeaders.IncludeCorrelationId.Should().BeFalse();
            responseHeaders.IncludeCallId.Should().BeFalse();
            responseHeaders.Cookies.Should().BeEmpty();
            responseHeaders.Overrides.Should().BeEmpty();
        }

        [TestMethod]
        public async Task New_OptionsFileModified_FileIsReloadedAsync()
        {
            try
            {
                File.Copy(@"Settings/OptionsOld.json", @"Settings/OptionsReload.json", true);

                var config = new ConfigurationBuilder()
                    .AddJsonFile(@"Settings/OptionsReload.json", optional: false, reloadOnChange: true)
                    .Build();

                var services = new ServiceCollection()
                    .AddTest()
                    .Configure<GatewayOptions>(config);

                serviceProvider = services.BuildServiceProvider();
                var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
                var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<GatewayOptions>>();

                var httpContext = new DefaultHttpContext();
                var settings = settingsProvider.CurrentValue.Routes.First();

                var context = new ProxyContext(
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

                    var sender = settings.Proxy!.UpstreamRequest.Sender;
                    sender.GetTimeout(context).TotalMilliseconds.Should().Be(5000);
                    sender.AllowAutoRedirect.Should().BeFalse();
                    sender.UseCookies.Should().BeTrue();
                    ValidateSocketsHttpHandler("api-route-http-client-name", sender);

                    File.Copy(@"Settings/OptionsNew.json", @"Settings/OptionsReload.json", true);

                    // this delay is needed because the lifetime of the http handler is set to 1 seconds. We have to wait for it to expire.
                    await Task.Delay(2000);

                    changeEvent.WaitOne();

                    settings = settingsProvider.CurrentValue.Routes.First();
                    context = new ProxyContext(
                        Substitute.For<ITraceIdProvider>(),
                        httpContext,
                        new Route(settings, string.Empty, ImmutableDictionary<string, string>.Empty));

                    sender = settings.Proxy!.UpstreamRequest.Sender;
                    sender.GetTimeout(context).TotalMilliseconds.Should().Be(2000);
                    sender.AllowAutoRedirect.Should().BeTrue();
                    sender.UseCookies.Should().BeFalse();
                    ValidateSocketsHttpHandler("api-route-http-client-name", sender);

                    File.Copy(@"Settings/OptionsOld.json", @"Settings/OptionsReload.json", true);

                    // this delay is needed because the lifetime of the http handler is set to 1 seconds. We have to wait for it to expire.
                    await Task.Delay(2000);

                    changeEvent.WaitOne();
                    settings = settingsProvider.CurrentValue.Routes.First();
                    context = new ProxyContext(
                        Substitute.For<ITraceIdProvider>(),
                        httpContext,
                        new Route(settings, string.Empty, ImmutableDictionary<string, string>.Empty));

                    sender = settings.Proxy!.UpstreamRequest.Sender;
                    sender.GetTimeout(context).TotalMilliseconds.Should().Be(5000);
                    sender.AllowAutoRedirect.Should().BeFalse();
                    sender.UseCookies.Should().BeTrue();
                    ValidateSocketsHttpHandler("api-route-http-client-name", sender);
                }
            }
            finally
            {
                File.Delete(@"Settings/OptionsReload.json");
            }
        }

        [TestMethod]
        public void New_UpstreamSenderWithExplicitHttpClientName_HttpClientIsCorrectlyCreated()
        {
            var context = GetProxyContext(@"Settings/OptionsSenderHttpClient.json");
            var requestSender = context.Route.Settings.Proxy!.UpstreamRequest.Sender;
            requestSender.HttpClientName.Should().Be("api-route-http-client-name");
            requestSender.GetTimeout(context).TotalMilliseconds.Should().Be(5200);
            requestSender.AllowAutoRedirect.Should().BeFalse();
            requestSender.UseCookies.Should().BeTrue();
            ValidateSocketsHttpHandler("api-route-http-client-name", requestSender);
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

        [TestMethod]
        public void New_InvalidCookieSameSiteValue_LogsErrorAndIgnoresBadCookie()
        {
            var options = new GatewayOptions();
            var route = new GatewayOptions.RouteOptions
            {
                Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                {
                    To = "/e/f/g/",
                    DownstreamResponse = new GatewayOptions.RouteOptions.ProxyOptions.DownstreamResponseOptions()
                }
            };

            route.Proxy.DownstreamResponse.Headers.Cookies.Add("test", new GatewayOptions.RouteOptions.ProxyOptions.DownstreamResponseOptions.HeadersOptions.CookieOptions
            {
                SameSite = "bad value",
            });

            options.Routes.Add("/a/b/c/", route);
            CreateSettingsAndCheckLogs(options, "The 'bad value' is not a valid value for 'test' cookie's SameSite attribute. Valid values are 'none', 'lax' and 'strict'.");
        }

        private GatewaySettings GetSettings(string jsonFile)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(jsonFile, optional: false)
                .Build();

            var services = new ServiceCollection()
                .AddTest()
                .Configure<GatewayOptions>(config);

            serviceProvider = services
                .BuildServiceProvider();

            return serviceProvider
                .GetRequiredService<ISettingsProvider>()
                .CurrentValue;
        }

        private ProxyContext GetProxyContext(GatewaySettings settings)
        {
            var httpContext = new DefaultHttpContext();
            var routeSettings = settings.Routes.First();

            return new ProxyContext(
                Substitute.For<ITraceIdProvider>(),
                httpContext,
                new Route(routeSettings, string.Empty, ImmutableDictionary<string, string>.Empty));
        }

        private ProxyContext GetProxyContext(string jsonFile)
        {
            var settings = GetSettings(jsonFile);
            return GetProxyContext(settings);
        }

        private GatewaySettings CreateSettings(GatewayOptions options)
        {
            var services = new ServiceCollection().AddTest().AddTestOptions(options);
            serviceProvider = services.BuildServiceProvider();
            var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
            return settingsProvider.CurrentValue;
        }

        private GatewaySettings CreateSettingsAndCheckLogs(GatewayOptions options, params string[] messages)
        {
            var services = new ServiceCollection().AddTest().AddTestOptions(options);
            serviceProvider = services.BuildServiceProvider();
            var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();

            var logger = (Logger<SettingsCreator>)serviceProvider.GetRequiredService<ILogger<SettingsCreator>>();
            foreach (var message in messages)
                logger.Logs.Any(l => l.ContainsOrdinalIgnoreCase(message)).Should().BeTrue();

            return settingsProvider.CurrentValue;
        }

        private void ValidateSocketsHttpHandler(string httpClientName, UpstreamRequestSenderSettings settings)
        {
            // Need to force expire the current handler
            var cache = serviceProvider!.GetRequiredService<IOptionsMonitorCache<HttpClientFactoryOptions>>();
            cache.GetOrAdd(
                httpClientName,
                () => throw new InvalidOperationException("Should never get here"))
                .HandlerLifetime = TimeSpan.FromSeconds(1);

            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(httpClientName);

            // The only way I can check the socket handler is through reflection
            var handler = typeof(HttpMessageInvoker)
                .GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(client) as HttpMessageHandler;

            handler.Should().NotBeNull();

            while (handler != null)
            {
                if (handler is SocketsHttpHandler socket)
                {
                    socket.ConnectTimeout.Should().Be(settings.ConnectTimeout);
                    socket.Expect100ContinueTimeout.Should().Be(settings.Expect100ContinueTimeout);
                    socket.PooledConnectionIdleTimeout.Should().Be(settings.PooledConnectionIdleTimeout);
                    socket.PooledConnectionLifetime.Should().Be(settings.PooledConnectionLifetime);
                    socket.ResponseDrainTimeout.Should().Be(settings.ResponseDrainTimeout);
                    socket.MaxAutomaticRedirections.Should().Be(settings.MaxAutomaticRedirections);
                    socket.MaxConnectionsPerServer.Should().Be(settings.MaxConnectionsPerServer);
                    socket.MaxResponseDrainSize.Should().Be(settings.MaxResponseDrainSizeInBytes);
                    socket.MaxResponseHeadersLength.Should().Be(settings.MaxResponseHeadersLengthInKilobytes);
                    socket.UseCookies.Should().Be(settings.UseCookies);
                    socket.AllowAutoRedirect.Should().Be(settings.AllowAutoRedirect);
                    return;
                }

                if (handler is DelegatingHandler delegating)
                {
                    handler = delegating.InnerHandler;
                    continue;
                }

                break;
            }

            throw new InvalidOperationException("We should never get here!");
        }
    }
}
