using System.Collections.Generic;

namespace EmbyKinopoiskTrailers.Api.KinopoiskDev.Model
{
    internal sealed class KpVideos
    {
        public List<KpVideo> Trailers { get; set; } = new List<KpVideo>();
        public List<KpVideo> Teasers { get; set; } = new List<KpVideo>();
    }
}
