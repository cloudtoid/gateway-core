namespace Cloudtoid.Foid.UnitTests
{
    using System.Linq;
    using Cloudtoid.Foid.Expression;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class VariableNamesTests
    {
        [TestMethod]
        public void IsValidVariableChar_AllValidChars_Success()
        {
            var name = "abcdefghijklmnopqrstvuwxyzABCDEFGHIJKLMNOPQRSTVUWXYZ0123456789_";
            name.All(c => c.IsValidVariableChar(false)).Should().BeTrue();
        }

        [TestMethod]
        public void IsValidVariableChar_WhenFirstCharIsNumber_Fails()
        {
            for (int i = 48; i < 59; i++)
                ((char)i).IsValidVariableChar(true).Should().BeFalse();
        }

        [TestMethod]
        public void IsValidVariableChar_WhenAnInvalidChar_Success()
        {
            Enumerable.Range(0, 128).Count(c => VariableNames.IsValidVariableChar(c, false)).Should().Be(63);
        }
    }
}
