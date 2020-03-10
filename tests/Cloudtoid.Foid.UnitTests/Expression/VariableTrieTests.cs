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
            var trie = new VariableTrieNode<string>();
            trie.AddValue("_", "_");

            foreach (char c in Enumerable.Range('a', 26))
                trie.AddValue(c.ToString(), c.ToString());

            foreach (char c in Enumerable.Range('A', 26))
                trie.AddValue(c.ToString(), c.ToString());

            foreach (char c in Enumerable.Range('a', 26))
                trie.GetValues(c.ToString()).Should().HaveCount(1);

            trie.GetValues("_").Should().HaveCount(1);
        }

        [TestMethod]
        public void Create_WhenNotSupportedCharacters_Throws()
        {
            var trie = new VariableTrieNode<string>();
            Action act = () => trie.AddValue("*", "throws");
            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public void Create_WhenSimpleTry_Success()
        {
            // Arrange
            var items = new[]
            {
                VariableNames.ContentLength,
                VariableNames.ContentType,
                VariableNames.CorrelationId,
                VariableNames.CallId,
                VariableNames.Host,
                VariableNames.RequestMethod,
                VariableNames.RequestScheme,
                VariableNames.RequestPathBase,
                VariableNames.RequestPath,
                VariableNames.RequestQueryString,
                VariableNames.RequestEncodedUri,
                VariableNames.RemoteAddress,
                VariableNames.RemotePort,
                VariableNames.ServerAddress,
                VariableNames.ServerName,
                VariableNames.ServerPort,
                VariableNames.ServerProtocol,
            };

            // Act
            var trie = new VariableTrieNode<string>();
            for (int i = 0; i < items.Length; i++)
                trie.AddValue(items[i], items[i]);

            // Assert
            trie.GetValues("content").Should().BeEmpty();
            trie.GetValues(string.Empty).Should().BeEmpty();
            trie.GetValues("*&-").Should().BeEmpty();

            trie.GetValues(VariableNames.RequestPathBase)
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        (VariableNames.RequestPathBase, VariableNames.RequestPathBase.Length),
                        (VariableNames.RequestPath, VariableNames.RequestPath.Length)
                    });

            foreach (var item in items)
            {
                trie.TryGetBestMatch(item, out var r, out var l).Should().BeTrue();
                r.Should().Be(item);
                l.Should().Be(item.Length);
            }

            var value = VariableNames.RequestPathBase;
            trie.TryGetBestMatch(value, out var res, out var lm).Should().BeTrue();
            res.Should().Be(value);
            lm.Should().Be(value.Length);

            trie.TryGetBestMatch(value + "abcd", out res, out lm).Should().BeTrue();
            res.Should().Be(value);
            lm.Should().Be(value.Length);
        }
    }
}
