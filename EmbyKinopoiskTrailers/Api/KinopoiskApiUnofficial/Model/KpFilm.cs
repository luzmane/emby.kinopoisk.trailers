using System.Diagnostics;

namespace EmbyKinopoiskTrailers.Api.KinopoiskApiUnofficial.Model
{
    [DebuggerDisplay("#{KinopoiskId}, {NameRu}")]
    internal sealed class KpFilm
    {
        public long KinopoiskId { get; set; }
        public string ImdbId { get; set; }
        public string Description { get; set; }
        public string NameRu { get; set; }
        public int? Year { get; set; }
        public string PosterUrl { get; set; }
        public string PosterUrlPreview { get; set; }
    }
}
