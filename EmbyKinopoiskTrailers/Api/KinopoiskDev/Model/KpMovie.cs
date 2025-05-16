using System.Diagnostics;

using EmbyKinopoiskTrailers.Api.Model;

namespace EmbyKinopoiskTrailers.Api.KinopoiskDev.Model
{
    [DebuggerDisplay("#{Id}, {Name}")]
    internal sealed class KpMovie
    {
        public string AlternativeName { get; set; }
        public KpExternalId ExternalId { get; set; }
        public long Id { get; set; }
        public string Name { get; set; }
        public KpMovieType? TypeNumber { get; set; }
        public KpVideos Videos { get; set; }
        public KpPremiere Premiere { get; set; }
        public string Description { get; set; }
        public KpImage Poster { get; set; }
    }
}
