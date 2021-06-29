using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cloudtoid.GatewayCore.Expression;
using Cloudtoid.GatewayCore.Settings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cloudtoid.GatewayCore.UnitTests
{
    [TestClass]
    public sealed class SettingsTests
    {
        private IServiceProvider? serviceProvider;

        [TestMethod]
        public void New_FullyPopulatedOptions_AllValuesAreReadCorrectly()
        {
            var context = GetProxyContext(@"Settings/OptionsFull.json");
            var settings = serviceProvider.GetRequiredService<ISettingsProvider>();
            settings.CurrentValue.System.RouteCacheMaxCount.Should().Be(1024);

            var routeSettings = context.Route.Settings;
            routeSettings.Route.Should().Be("/api/");

            context.ProxyName.Should().Be("some-proxy-name");
            routeSettings.Proxy!.EvaluateCorrelationIdHeader(context).Should().Be("x-request-id");

            var request = routeSettings.Proxy.UpstreamRequest;
            request.EvaluateHttpVersion(context).Should().Be(HttpVersion.Version30);

            var requestHeaders = request.Headers;
            requestHeaders.DiscardEmpty.Should().BeTrue();
            requestHeaders.DiscardUnderscore.Should().BeTrue();
            requestHeaders.DiscardInboundHeaders.Should().BeTrue();
            requestHeaders.SkipVia.Should().BeTrue();
            requestHeaders.SkipCorrelationId.Should().BeTrue();
            requestHeaders.SkipCallId.Should().BeTrue();
            requestHeaders.SkipForwarded.Should().BeTrue();
            requestHeaders.UseXForwarded.Should().BeTrue();
            requestHeaders.AddExternalAddress.Should().BeTrue();
            requestHeaders.Overrides.Values.Select(h => (h.Name, Values: h.EvaluateValues(context)))
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (Name: "x-extra-1", Values: new[] { "value1_1", "value1_2" }),
                        (Name: "x-extra-2", Values: new[] { "value2_1", "value2_2" })
                    });
            requestHeaders.Discards.Should().BeEquivalentTo("x-discard-1", "x-discard-2");

            var requestSender = request.Sender;
            requestSender.HttpClientName.Should().Be("api-route-http-client-name");
            requestSender.EvaluateTimeout(context).TotalMilliseconds.Should().Be(5200);
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
            responseHeaders.DiscardInboundHeaders.Should().BeTrue();
            responseHeaders.SkipVia.Should().BeTrue();
            responseHeaders.AddCorrelationId.Should().BeTrue();
            responseHeaders.AddCallId.Should().BeTrue();
            responseHeaders.AddServer.Should().BeTrue();
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
            responseHeaders.Overrides.Values.Select(h => (h.Name, Values: h.EvaluateValues(context)))
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (Name: "x-extra-1", Values: new[] { "value1_1", "value1_2" }),
                        (Name: "x-extra-2", Values: new[] { "value2_1", "value2_2" })
                    });
            responseHeaders.Discards.Should().BeEquivalentTo("x-discard-1", "x-discard-2");
        }

        [TestMethod]
        public void New_AllOptionsThatAllowExpressions_AllValuesAreEvaluatedCorrectly()
        {
            var context = GetProxyContext(@"Settings/OptionsWithExpressions.json");
            var settings = context.Route.Settings;

            var expressionValue = Environment.MachineName;
            context.ProxyName.Should().Be("ProxyName:" + expressionValue);
            settings.Proxy!.EvaluateCorrelationIdHeader(context).Should().Be("CorrelationIdHeader:" + expressionValue);

            var request = settings.Proxy.UpstreamRequest;
            request.EvaluateHttpVersion(context).Should().Be(HttpVersion.Version11);

            var requestHeaders = request.Headers;
            requestHeaders.Overrides.Values.Select(h => (h.Name, Values: h.EvaluateValues(context)))
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (Name: "x-extra-1", Values: new[] { "x-extra-1:v1:" + expressionValue, "x-extra-1:v2:" + expressionValue }),
                        (Name: "x-extra-2", Values: new[] { "x-extra-2:v1:" + expressionValue, "x-extra-2:v2:" + expressionValue })
                    });

            var requestSender = request.Sender;
            requestSender.EvaluateTimeout(context).TotalMilliseconds.Should().Be(5200);

            var response = settings.Proxy.DownstreamResponse;
            var responseHeaders = response.Headers;
            responseHeaders.Overrides.Values.Select(h => (h.Name, Values: h.EvaluateValues(context)))
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (Name: "x-extra-1", Values: new[] { "x-extra-1:v1:" + expressionValue, "x-extra-1:v2:" + expressionValue }),
                        (Name: "x-extra-2", Values: new[] { "x-extra-2:v1:" + expressionValue, "x-extra-2:v2:" + expressionValue })
                    });

            responseHeaders.Cookies.Values.Single().EvaluateDomain(context).Should().Be(expressionValue + ".com");
        }

        [TestMethod]
        public void New_EmptyOptions_AllValuesSetToDefault()
        {
            var context = GetProxyContext(@"Settings/OptionsEmpty.json");
            var settings = serviceProvider.GetRequiredService<ISettingsProvider>();
            settings.CurrentValue.System.RouteCacheMaxCount.Should().Be(100000);

            context.ProxyName.Should().Be("gwcore");
            var routeSettings = context.Route.Settings;

            var request = routeSettings.Proxy!.UpstreamRequest;
            request.EvaluateHttpVersion(context).Should().Be(HttpVersion.Version20);

            var requestHeaders = request.Headers;
            requestHeaders.DiscardEmpty.Should().BeFalse();
            requestHeaders.DiscardUnderscore.Should().BeFalse();
            requestHeaders.DiscardInboundHeaders.Should().BeFalse();
            requestHeaders.SkipVia.Should().BeFalse();
            requestHeaders.SkipCorrelationId.Should().BeFalse();
            requestHeaders.SkipCallId.Should().BeFalse();
            requestHeaders.SkipForwarded.Should().BeFalse();
            requestHeaders.UseXForwarded.Should().BeFalse();
            requestHeaders.AddExternalAddress.Should().BeFalse();
            requestHeaders.Overrides.Should().BeEmpty();
            requestHeaders.Discards.Should().BeEmpty();

            var requestSender = request.Sender;
            requestSender.HttpClientName.Should().Be(GuidProvider.StringValue);
            requestSender.EvaluateTimeout(context).TotalMilliseconds.Should().Be(240000);
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
            responseHeaders.DiscardEmpty.Should().BeFalse();
            responseHeaders.DiscardUnderscore.Should().BeFalse();
            responseHeaders.DiscardInboundHeaders.Should().BeFalse();
            responseHeaders.SkipVia.Should().BeFalse();
            responseHeaders.AddCorrelationId.Should().BeFalse();
            responseHeaders.AddCallId.Should().BeFalse();
            responseHeaders.AddServer.Should().BeFalse();
            responseHeaders.Cookies.Should().BeEmpty();
            responseHeaders.Overrides.Should().BeEmpty();
            responseHeaders.Discards.Should().BeEmpty();
        }

        [TestMethod]
        public async Task New_OptionsFileModified_FileIsReloadedAsync()
        {
            try
            {
                File.Copy(@"Settings/OptionsOld.json", @"Settings/OptionsReload.json", true);

                var services = new ServiceCollection().AddTest(
                    gatewayOptionsJsonFile: @"Settings/OptionsReload.json",
                    reloadOnGatewayOptionsJsonFileChange: true);

                serviceProvider = services.BuildServiceProvider();
                var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
                var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<GatewayOptions>>();
                var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();

                var context = serviceProvider.GetProxyContext();

                using (var changeEvent = new AutoResetEvent(false))
                {
                    void Reset(object o)
                    {
                        changeEvent.Set();
                        monitor.OnChange(Reset);
                    }

                    monitor.OnChange(Reset);

                    var sender = settingsProvider.CurrentValue.Routes[0].Proxy.UpstreamRequest.Sender;
                    sender.EvaluateTimeout(context).TotalMilliseconds.Should().Be(5000);
                    sender.AllowAutoRedirect.Should().BeFalse();
                    sender.UseCookies.Should().BeTrue();
                    ValidateSocketsHttpHandler("api-route-http-client-name", sender);

                    File.Copy(@"Settings/OptionsNew.json", @"Settings/OptionsReload.json", true);

                    // this delay is needed because the lifetime of the http handler is set to 1 seconds. We have to wait for it to expire.
                    await Task.Delay(2000);

                    changeEvent.WaitOne();

                    context = serviceProvider.GetProxyContext();
                    sender = settingsProvider.CurrentValue.Routes[0].Proxy.UpstreamRequest.Sender;
                    sender.EvaluateTimeout(context).TotalMilliseconds.Should().Be(2000);
                    sender.AllowAutoRedirect.Should().BeTrue();
                    sender.UseCookies.Should().BeFalse();
                    ValidateSocketsHttpHandler("api-route-http-client-name", sender);

                    File.Copy(@"Settings/OptionsOld.json", @"Settings/OptionsReload.json", true);

                    // this delay is needed because the lifetime of the http handler is set to 1 seconds. We have to wait for it to expire.
                    await Task.Delay(2000);

                    changeEvent.WaitOne();
                    context = serviceProvider.GetProxyContext();
                    sender = settingsProvider.CurrentValue.Routes[0].Proxy.UpstreamRequest.Sender;
                    sender.EvaluateTimeout(context).TotalMilliseconds.Should().Be(5000);
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
            requestSender.EvaluateTimeout(context).TotalMilliseconds.Should().Be(5200);
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
                            Overrides = new Dictionary<string, string[]>()
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

        [TestMethod]
        public void New_NullAndEmptyOverride_CorrectError()
        {
            var options = new GatewayOptions();
            var route = new GatewayOptions.RouteOptions
            {
                Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                {
                    To = "/e/f/g/"
                }
            };

            route.Proxy.DownstreamResponse.Headers.Overrides.Add("x-null", null!);
            route.Proxy.DownstreamResponse.Headers.Overrides.Add("x-empty", Array.Empty<string>());

            options.Routes.Add("/a/b/c/", route);
            CreateSettingsAndCheckLogs(
                options,
                "The 'x-null' is either null or empty. It will be ignored.",
                "The 'x-empty' is either null or empty. It will be ignored.");
        }

        private ProxyContext GetProxyContext(string jsonFile)
        {
            var services = new ServiceCollection().AddTest(jsonFile);
            serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetProxyContext();
        }

        private GatewaySettings CreateSettings(GatewayOptions options)
        {
            var services = new ServiceCollection().AddTest(gatewayOptions: options);
            serviceProvider = services.BuildServiceProvider();
            var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
            return settingsProvider.CurrentValue;
        }

        private GatewaySettings CreateSettingsAndCheckLogs(GatewayOptions options, params string[] messages)
        {
            var services = new ServiceCollection().AddTest(gatewayOptions: options);
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

            while (handler is not null)
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
