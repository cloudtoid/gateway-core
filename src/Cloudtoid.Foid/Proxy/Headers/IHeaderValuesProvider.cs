namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.Extensions.Primitives;

    public interface IHeaderValuesProvider
    {
        bool AllowHeadersWithEmptyValue { get; }

        bool AllowHeadersWithUnderscoreInName { get; }

        bool TryGetHeaderValues(string name, StringValues currentValues, out StringValues values);
    }
}
