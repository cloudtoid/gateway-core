namespace Cloudtoid.Foid.Routes.Pattern
{
    internal interface IPatternCompiler
    {
        CompiledPattern Compile(PatternNode pattern);
    }
}
