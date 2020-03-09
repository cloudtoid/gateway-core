namespace Cloudtoid.Foid.UnitTests
{
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public sealed class ExpressionEvaluatorTests
    {
        [TestMethod]
        public void Evaluate_EmptyExpression_NothingToEvaluate()
        {
            Evaluate(string.Empty).Should().Be(string.Empty);
        }

        [TestMethod]
        public void Evaluate_NullExpression_NothingToEvaluate()
        {
            Evaluate(null).Should().Be(null);
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
            Evaluate(" " + GetVarName(VariableNames.ContentLength) + " ", context).Should().Be("100");
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
        public void Evaluate_InvalidCharAfterVariableName_ReturnsOriginalExpression()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentLength = 100;
            var expr = GetVarName(VariableNames.ContentLength + ">10");
            Evaluate(expr, context).Should().Be("100");
        }

        [TestMethod]
        public void Evaluate_ContentLengthVariable_Evaluated()
        {
            var context = new DefaultHttpContext();
            context.Request.ContentLength = 100;
            Evaluate(GetVarName(VariableNames.ContentLength), context).Should().Be("100");
        }

        private static string GetVarName(string varName) => $"${varName}";

        private static string? Evaluate(string? expression, HttpContext? context = null, FoidOptions? options = null)
        {
            var monitor = Substitute.For<IOptionsMonitor<FoidOptions>>();
            monitor.CurrentValue.Returns(options ?? new FoidOptions());
            var evaluator = new ExpressionEvaluator(
                new TraceIdProvider(GuidProvider.Instance, monitor),
                new HostProvider(monitor));

            context ??= new DefaultHttpContext();
            return evaluator.Evaluate(context, expression);
        }
    }
}
