using System.Diagnostics;

namespace EmbyKinopoiskTrailers.Api.Model
{
    [DebuggerDisplay("{Name} ({MoviesCount})")]
    internal sealed class KpLists
    {
        public string Category { get; set; }
        public KpImage Cover { get; set; }
        public int MoviesCount { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
    }
}
