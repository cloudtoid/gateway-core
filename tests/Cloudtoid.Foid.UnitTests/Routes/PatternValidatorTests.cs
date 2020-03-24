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
            error.Should().Contain($"Found two consecutive '{PatternConstants.SegmentStart}' which is invalid.");
        }

        [TestMethod]
        public void Validate_WhenConsecutiveSegmentStartOneOptional_Fails()
        {
            validator.Validate(Parse("/(/)"), out var error).Should().BeFalse();
            error.Should().Contain($"Found two consecutive '{PatternConstants.SegmentStart}' which is invalid.");
        }

        [TestMethod]
        public void Validate_WhenConsecutiveWildcard_Fails()
        {
            validator.Validate(Parse("**"), out var error).Should().BeFalse();
            error.Should().Contain($"Found two consecutive '{PatternConstants.Wildcard}' which is invalid.");
        }

        [TestMethod]
        public void Validate_WhenConsecutiveWildcardOneOptional_Fails()
        {
            validator.Validate(Parse("*(*)"), out var error).Should().BeFalse();
            error.Should().Contain($"Found two consecutive '{PatternConstants.Wildcard}' which is invalid.");
        }

        private static PatternNode Parse(string pattern)
        {
            var parser = new PatternParser();
            parser.TryParse(pattern, out var result, out var error).Should().BeTrue();
            error.Should().BeNull();
            return result!;
        }
    }
}
