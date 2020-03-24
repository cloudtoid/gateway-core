namespace Cloudtoid.Foid.UnitTests
{
    using Cloudtoid.Foid.Routes.Pattern;
    using FluentAssertions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class PatternValidatorTests
    {
        private readonly IPatternValidator validator;

        public PatternValidatorTests()
        {
            var services = new ServiceCollection().AddTest();
            var serviceProvider = services.BuildServiceProvider();
            validator = serviceProvider.GetRequiredService<IPatternValidator>();
        }

        [TestMethod]
        public void Validate_WhenConsecutiveSegmentStart_Fails()
        {
            validator.Validate(Parse("//"), out var error).Should().BeFalse();
            error.Should().Contain($"Found consecutive '{PatternConstants.SegmentStart}' which is invalid.");
        }

        [TestMethod]
        public void Validate_WhenConsecutiveSegmentStartOneOptional_Fails()
        {
            validator.Validate(Parse("/(/)"), out var error).Should().BeFalse();
            error.Should().Contain($"Found consecutive '{PatternConstants.SegmentStart}' which is invalid.");
        }

        [TestMethod]
        public void Validate_WhenConsecutiveWildcard_Fails()
        {
            validator.Validate(Parse("**"), out var error).Should().BeFalse();
            error.Should().Contain($"Found consecutive '{PatternConstants.Wildcard}' which is invalid.");
        }

        [TestMethod]
        public void Validate_WhenConsecutiveWildcardOneOptional_Fails()
        {
            validator.Validate(Parse("*(*)"), out var error).Should().BeFalse();
            error.Should().Contain($"Found consecutive '{PatternConstants.Wildcard}' which is invalid.");
        }

        [TestMethod]
        public void Validate_WhenTwoVariablesInSegment_Fails()
        {
            validator.Validate(Parse(":var1:var2"), out var error).Should().BeFalse();
            error.Should().Contain("Each URL segment can only include a single variable definition.");
        }

        [TestMethod]
        public void Validate_WhenMultiSegmentsAndTwoVariablesInSegment_Fails()
        {
            validator.Validate(Parse(":var0/:var1:var2"), out var error).Should().BeFalse();
            error.Should().Contain("Each URL segment can only include a single variable definition.");
        }

        [TestMethod]
        public void Validate_WhenMultiSegmentsButSingleVariableInEach_NoFailure()
        {
            validator.Validate(Parse(":var0/:var1/:var2"), out var error).Should().BeTrue();
            error.Should().BeNull();
        }

        [TestMethod]
        public void Validate_WhenWildcardFollowsVariable_Fail()
        {
            validator.Validate(Parse(":var0*/"), out var error).Should().BeFalse();
            error.Should().Contain($"The wild-card character '{PatternConstants.Wildcard}' cannot not follow a variable.");
        }

        [TestMethod]
        public void Validate_WhenWildcardFollowsVariableButNotImmediately_Fail()
        {
            validator.Validate(Parse(":var0/*"), out var error).Should().BeTrue();
            error.Should().BeNull();
        }

        // TODO: More tests and validators:
        // *placeholder:var0/    -- perfectly valid

        private static PatternNode Parse(string pattern)
        {
            var parser = new PatternParser();
            parser.TryParse(pattern, out var result, out var error).Should().BeTrue();
            error.Should().BeNull();
            return result!;
        }
    }
}
