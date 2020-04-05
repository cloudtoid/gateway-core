namespace Cloudtoid.GatewayCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Cloudtoid.GatewayCore.Expression;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ExpressionEvaluatorTests
    {
        private readonly IServiceCollection services = new ServiceCollection();
        private IServiceProvider? serviceProvider;

        [TestMethod]
        public void Evaluate_EmptyExpression_NothingToEvaluate()
        {
            Evaluate(string.Empty).Should().Be(string.Empty);
        }

        [TestMethod]
        public void Evaluate_WhiteSpaceExpression_NothingToEvaluate()
        {
            Evaluate("  ").Should().Be("  ");
        }

        [TestMethod]
        public void Evaluate_VariableNameNeedingToGetTrimmedExpression_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentLength = 100;
            Evaluate(" " + GetVarName(SystemVariableNames.ContentLength) + " ", context).Should().Be(" 100 ");
        }

        [TestMethod]
        public void Evaluate_UnknownVariable_ReturnsOriginalExpression()
        {
            var context = new DefaultHttpContext();
            var expr = GetVarName("test-var");
            Evaluate(expr, context).Should().Be(expr);
        }

        [TestMethod]
        public void Evaluate_EmptyVariable_ReturnsOriginalExpression()
        {
            var context = new DefaultHttpContext();
            var expr = GetVarName(string.Empty);
            Evaluate(expr, context).Should().Be(expr);
        }

        [TestMethod]
        public void Evaluate_InvalidCharInVariableName_ReturnsOriginalExpression()
        {
            var context = new DefaultHttpContext();
            var expr = GetVarName("<>");
            Evaluate(expr, context).Should().Be(expr);
        }

        [TestMethod]
        public void Evaluate_WhenVariableNameGreaterThanValue_PartialEval()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentLength = 100;
            var expr = GetVarName(SystemVariableNames.ContentLength + ">10");
            Evaluate(expr, context).Should().Be("100>10");
        }

        [TestMethod]
        public void Evaluate_ContentLengthVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentLength = 100;
            Evaluate(GetVarName(SystemVariableNames.ContentLength), context).Should().Be("100");
        }

        [TestMethod]
        public void Evaluate_ContentTypeVariable_Evaluated()
        {
            const string value = "text/html";
            var context = new DefaultHttpContext();
            context.Request.ContentType = value;
            Evaluate(GetVarName(SystemVariableNames.ContentType), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_CorrelationIdVariable_Evaluated()
        {
            const string value = "abc";
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("x-correlation-id", value);
            Evaluate(GetVarName(SystemVariableNames.CorrelationId), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_CallIdVariable_Evaluated()
        {
            Evaluate(GetVarName(SystemVariableNames.CallId)).Should().Be(GuidProvider.Value.ToStringInvariant("N"));
        }

        [TestMethod]
        public void Evaluate_HostVariable_Evaluated()
        {
            const string value = "abc";
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Host, value);
            Evaluate(GetVarName(SystemVariableNames.Host), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestMethodVariable_Evaluated()
        {
            string value = HttpMethods.Get;
            var context = new DefaultHttpContext();
            context.Request.Method = value;
            Evaluate(GetVarName(SystemVariableNames.RequestMethod), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestPathBaseVariable_Evaluated()
        {
            const string value = "/api";
            var context = new DefaultHttpContext();
            context.Request.PathBase = new PathString(value);
            Evaluate(GetVarName(SystemVariableNames.RequestPathBase), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestPathVariable_Evaluated()
        {
            const string value = "/repos";
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString(value);
            Evaluate(GetVarName(SystemVariableNames.RequestPath), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestQueryStringVariable_Evaluated()
        {
            const string value = "?a=10&b=20";
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString(value);
            Evaluate(GetVarName(SystemVariableNames.RequestQueryString), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestEncodedUrlVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("cloudtoid.com");
            context.Request.PathBase = new PathString("/api");
            context.Request.Path = new PathString("/repos");
            context.Request.QueryString = new QueryString("?a=10&b=20");
            Evaluate(GetVarName(SystemVariableNames.RequestEncodedUrl), context).Should().Be("https://cloudtoid.com/api/repos?a=10&b=20");
        }

        [TestMethod]
        public void Evaluate_RemoteAddressVariable_Evaluated()
        {
            const string value = "1.2.3.4";
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(value);
            Evaluate(GetVarName(SystemVariableNames.RemoteAddress), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RemotePortVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemotePort = 10;
            Evaluate(GetVarName(SystemVariableNames.RemotePort), context).Should().Be("10");
        }

        [TestMethod]
        public void Evaluate_RequestSchemeVariable_Evaluated()
        {
            const string value = "HTTPS";
            var context = new DefaultHttpContext();
            context.Request.Scheme = value;
            Evaluate(GetVarName(SystemVariableNames.RequestScheme), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_ServerNameVariable_Evaluated()
        {
            Evaluate(GetVarName(SystemVariableNames.ServerName)).Should().Be(Environment.MachineName);
        }

        [TestMethod]
        public void Evaluate_ServerAddressVariable_Evaluated()
        {
            const string value = "1.2.3.4";
            var context = new DefaultHttpContext();
            context.Connection.LocalIpAddress = IPAddress.Parse(value);
            Evaluate(GetVarName(SystemVariableNames.ServerAddress), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_ServerPortVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Connection.LocalPort = 10;
            Evaluate(GetVarName(SystemVariableNames.ServerPort), context).Should().Be("10");
        }

        [TestMethod]
        public void Evaluate_ServerProtocolVariable_Evaluated()
        {
            const string value = "HTTP/2.0";
            var context = new DefaultHttpContext();
            context.Request.Protocol = value;
            Evaluate(GetVarName(SystemVariableNames.ServerProtocol), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RouteVariable_Evaluated()
        {
            const string value = "some-prod-id";
            var options = TestExtensions.CreateDefaultOptions("/product/:id");
            var variables = new Dictionary<string, string> { ["id"] = value };
            Evaluate(GetVarName("id"), options: options, variables: variables).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RouteVariableThatDoesntExistInPattern_EvaluatedToNull()
        {
            var options = TestExtensions.CreateDefaultOptions("/product/");
            var variables = new Dictionary<string, string> { ["id"] = "some-prod-id" };
            Evaluate("$id", options: options, variables: variables).Should().Be("$id");
        }

        [TestMethod]
        public void Evaluate_RouteVariables_Evaluated()
        {
            const string idValue = "some-prod-id";
            const string catValue = "some-cat-id";
            var options = TestExtensions.CreateDefaultOptions("/category/:catid/product/:id");
            var variables = new Dictionary<string, string>
            {
                ["id"] = idValue,
                ["catid"] = catValue
            };

            Evaluate("cat-id = $catid, id = $id", options: options, variables: variables)
                .Should().Be($"cat-id = {catValue}, id = {idValue}");
        }

        [TestMethod]
        public void Evaluate_WhenMultipleVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("cloudtoid.com");
            context.Request.PathBase = new PathString("/api");
            context.Request.Path = new PathString("/repos");
            context.Request.QueryString = new QueryString("?a=10&b=20");
            Evaluate($"url = ${SystemVariableNames.RequestScheme}://${SystemVariableNames.Host}${SystemVariableNames.RequestPathBase}${SystemVariableNames.RequestPath}${SystemVariableNames.RequestQueryString}&c=30", context)
                .Should()
                .Be("url = https://cloudtoid.com/api/repos?a=10&b=20&c=30");
        }

        [TestMethod]
        public void Evaluate_WhenVariableNameAttachedToText_VariableIsEvaluatedAndExtraTextIsKept()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            Evaluate($"${SystemVariableNames.RequestScheme}test", context)
                .Should()
                .Be("httpstest");
        }

        [TestMethod]
        public void Evaluate_WhenVariableNameAttachedToTextAndAnotherVariable_VariablesAreEvaluatedAndExtraTextIsKept()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            Evaluate($"${SystemVariableNames.RequestScheme}test${SystemVariableNames.RequestScheme}", context)
                .Should()
                .Be("httpstesthttps");
        }

        [TestMethod]
        public void Evaluate_SameExpressionMultipleTimes_MustUseParserCache()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";

            Evaluate("abc", context).Should().Be("abc");
            Evaluate("abc", context).Should().Be("abc");
            Evaluate("abc", context).Should().Be("abc");

            var logger = (Logger<ExpressionEvaluator>)serviceProvider.GetRequiredService<ILogger<ExpressionEvaluator>>();
            logger.Logs.Where(l => l.ContainsOrdinalIgnoreCase("Parsing an expression: abc")).Should().HaveCount(1);
        }

        private static string GetVarName(string varName) => $"${varName}";

        private string Evaluate(
            string expression,
            HttpContext? httpContext = null,
            ReverseProxyOptions? options = null,
            IReadOnlyDictionary<string, string>? variables = null)
        {
            services.AddTest().AddTestOptions(options);
            serviceProvider = services.BuildServiceProvider();
            var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
            var context = serviceProvider.GetProxyContext(httpContext, variables: variables);
            return evaluator.Evaluate(context, expression);
        }
    }
}
