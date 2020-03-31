namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using System.Linq;
    using Cloudtoid.Foid.Expression;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class VariableTrieTests
    {
        [TestMethod]
        public void Create_WhenSupportedCharacters_DoesNotThrow()
        {
            var trie = new VariableTrie<string>();
            trie.AddValue("_", "_");

            foreach (char c in Enumerable.Range('a', 26))
                trie.AddValue(c.ToString(), c.ToString());

            foreach (char c in Enumerable.Range('A', 26))
                trie.AddValue(c.ToString(), c.ToString());

            foreach (char c in Enumerable.Range('0', 10))
                trie.AddValue(c.ToString(), c.ToString());

            foreach (char c in Enumerable.Range('a', 26))
                trie.GetMatches(c.ToString()).Should().HaveCount(1);

            foreach (char c in Enumerable.Range('0', 10))
                trie.GetMatches(c.ToString()).Should().HaveCount(1);

            trie.GetMatches("_").Should().HaveCount(1);
        }

        [TestMethod]
        public void Create_WhenNotSupportedCharacters_Throws()
        {
            var trie = new VariableTrie<string>();
            Action act = () => trie.AddValue("*", "throws");
            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public void Create_WhenSimpleTry_Success()
        {
            // Arrange
            var items = new[]
            {
                SystemVariableNames.ContentLength,
                SystemVariableNames.ContentType,
                SystemVariableNames.CorrelationId,
                SystemVariableNames.CallId,
                SystemVariableNames.Host,
                SystemVariableNames.RequestMethod,
                SystemVariableNames.RequestScheme,
                SystemVariableNames.RequestPathBase,
                SystemVariableNames.RequestPath,
                SystemVariableNames.RequestQueryString,
                SystemVariableNames.RequestEncodedUri,
                SystemVariableNames.RemoteAddress,
                SystemVariableNames.RemotePort,
                SystemVariableNames.ServerAddress,
                SystemVariableNames.ServerName,
                SystemVariableNames.ServerPort,
                SystemVariableNames.ServerProtocol,
            };

            // Act
            var trie = new VariableTrie<string>();
            for (int i = 0; i < items.Length; i++)
                trie.AddValue(items[i], items[i]);

            // Assert
            trie.GetMatches("content").Should().BeEmpty();
            trie.GetMatches(string.Empty).Should().BeEmpty();
            trie.GetMatches("*&-").Should().BeEmpty();
            trie.TryGetBestMatch("content", out _, out _).Should().BeFalse();
            trie.TryGetBestMatch(string.Empty, out _, out _).Should().BeFalse();
            trie.TryGetBestMatch("*&-", out _, out _).Should().BeFalse();

            trie.GetMatches(SystemVariableNames.RequestPathBase)
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (SystemVariableNames.RequestPathBase, SystemVariableNames.RequestPathBase.Length),
                        (SystemVariableNames.RequestPath, SystemVariableNames.RequestPath.Length)
                    });

            foreach (var item in items)
            {
                trie.TryGetBestMatch(item, out var r, out var l).Should().BeTrue();
                r.Should().Be(item);
                l.Should().Be(item.Length);
            }

            var value = SystemVariableNames.RequestPathBase;
            trie.TryGetBestMatch(value, out var res, out var lm).Should().BeTrue();
            res.Should().Be(value);
            lm.Should().Be(value.Length);

            trie.TryGetBestMatch(value + "abcd", out res, out lm).Should().BeTrue();
            res.Should().Be(value);
            lm.Should().Be(value.Length);
        }
    }
}
