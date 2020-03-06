namespace Cloudtoid.Foid
{
    using System;

    public class ExtraHeader
    {
        public string Key { get; set; } = string.Empty;

        public string[] Values { get; set; } = Array.Empty<string>();
    }
}