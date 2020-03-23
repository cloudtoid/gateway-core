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
            name.All(c => c.IsValidVariableChar()).Should().BeTrue();
        }

        [TestMethod]
        public void IsValidVariableChar_AnInvalidChar_Success()
        {
            Enumerable.Range(0, 128).Count(c => VariableNames.IsValidVariableChar(c)).Should().Be(63);
        }
    }
}
