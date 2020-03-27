namespace Cloudtoid.UrlPattern
{
    using System.Diagnostics.CodeAnalysis;

    public interface IPatternCompiler
    {
        bool TryCompile(
            string pattern,
            [NotNullWhen(true)] out CompiledPattern? compiledPattern,
            [NotNullWhen(false)] out string? errors);
    }
}
