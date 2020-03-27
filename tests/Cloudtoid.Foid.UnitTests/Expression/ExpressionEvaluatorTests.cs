namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using System.Net;
    using Cloudtoid.Foid.Expression;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ExpressionEvaluatorTests
    {
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
            Evaluate(" " + GetVarName(VariableNames.ContentLength) + " ", context).Should().Be(" 100 ");
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
            var expr = GetVarName(VariableNames.ContentLength + ">10");
            Evaluate(expr, context).Should().Be("100>10");
        }

        [TestMethod]
        public void Evaluate_ContentLengthVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentLength = 100;
            Evaluate(GetVarName(VariableNames.ContentLength), context).Should().Be("100");
        }

        [TestMethod]
        public void Evaluate_ContentTypeVariable_Evaluated()
        {
            const string value = "text/html";
            var context = new DefaultHttpContext();
            context.Request.ContentType = value;
            Evaluate(GetVarName(VariableNames.ContentType), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_CorrelationIdVariable_Evaluated()
        {
            const string value = "abc";
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("x-correlation-id", value);
            Evaluate(GetVarName(VariableNames.CorrelationId), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_CallIdVariable_Evaluated()
        {
            Evaluate(GetVarName(VariableNames.CallId)).Should().Be(GuidProvider.Value.ToStringInvariant("N"));
        }

        [TestMethod]
        public void Evaluate_HostVariable_Evaluated()
        {
            const string value = "abc";
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderNames.Host, value);
            Evaluate(GetVarName(VariableNames.Host), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestMethodVariable_Evaluated()
        {
            string value = HttpMethods.Get;
            var context = new DefaultHttpContext();
            context.Request.Method = value;
            Evaluate(GetVarName(VariableNames.RequestMethod), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestPathBaseVariable_Evaluated()
        {
            const string value = "/api";
            var context = new DefaultHttpContext();
            context.Request.PathBase = new PathString(value);
            Evaluate(GetVarName(VariableNames.RequestPathBase), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestPathVariable_Evaluated()
        {
            const string value = "/repos";
            var context = new DefaultHttpContext();
            context.Request.Path = new PathString(value);
            Evaluate(GetVarName(VariableNames.RequestPath), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestQueryStringVariable_Evaluated()
        {
            const string value = "?a=10&b=20";
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString(value);
            Evaluate(GetVarName(VariableNames.RequestQueryString), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RequestEncodedUriVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("cloudtoid.com");
            context.Request.PathBase = new PathString("/api");
            context.Request.Path = new PathString("/repos");
            context.Request.QueryString = new QueryString("?a=10&b=20");
            Evaluate(GetVarName(VariableNames.RequestEncodedUri), context).Should().Be("https://cloudtoid.com/api/repos?a=10&b=20");
        }

        [TestMethod]
        public void Evaluate_RemoteAddressVariable_Evaluated()
        {
            const string value = "1.2.3.4";
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(value);
            Evaluate(GetVarName(VariableNames.RemoteAddress), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_RemotePortVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemotePort = 10;
            Evaluate(GetVarName(VariableNames.RemotePort), context).Should().Be("10");
        }

        [TestMethod]
        public void Evaluate_RequestSchemeVariable_Evaluated()
        {
            const string value = "HTTPS";
            var context = new DefaultHttpContext();
            context.Request.Scheme = value;
            Evaluate(GetVarName(VariableNames.RequestScheme), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_ServerNameVariable_Evaluated()
        {
            Evaluate(GetVarName(VariableNames.ServerName)).Should().Be(Environment.MachineName);
        }

        [TestMethod]
        public void Evaluate_ServerAddressVariable_Evaluated()
        {
            const string value = "1.2.3.4";
            var context = new DefaultHttpContext();
            context.Connection.LocalIpAddress = IPAddress.Parse(value);
            Evaluate(GetVarName(VariableNames.ServerAddress), context).Should().Be(value);
        }

        [TestMethod]
        public void Evaluate_ServerPortVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Connection.LocalPort = 10;
            Evaluate(GetVarName(VariableNames.ServerPort), context).Should().Be("10");
        }

        [TestMethod]
        public void Evaluate_ServerProtocolVariable_Evaluated()
        {
            const string value = "HTTP/2.0";
            var context = new DefaultHttpContext();
            context.Request.Protocol = value;
            Evaluate(GetVarName(VariableNames.ServerProtocol), context).Should().Be(value);
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
            Evaluate($"url = ${VariableNames.RequestScheme}://${VariableNames.Host}${VariableNames.RequestPathBase}${VariableNames.RequestPath}${VariableNames.RequestQueryString}&c=30", context)
                .Should()
                .Be("url = https://cloudtoid.com/api/repos?a=10&b=20&c=30");
        }

        [TestMethod]
        public void Evaluate_WhenVariableNameAttachedToText_VariableIsEvaluatedAndExtraTextIsKept()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            Evaluate($"${VariableNames.RequestScheme}test", context)
                .Should()
                .Be("httpstest");
        }

        [TestMethod]
        public void Evaluate_WhenVariableNameAttachedToTextAndAnotherVariable_VariablesAreEvaluatedAndExtraTextIsKept()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            Evaluate($"${VariableNames.RequestScheme}test${VariableNames.RequestScheme}", context)
                .Should()
                .Be("httpstesthttps");
        }

        private static string GetVarName(string varName) => $"${varName}";

        private static string Evaluate(
            string expression,
            HttpContext? httpContext = null)
        {
            var services = new ServiceCollection().AddTest().AddTestOptions();
            var serviceProvider = services.BuildServiceProvider();
            var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
            var context = serviceProvider.GetProxyContext(httpContext);
            return evaluator.Evaluate(context, expression);
        }
    }
}
