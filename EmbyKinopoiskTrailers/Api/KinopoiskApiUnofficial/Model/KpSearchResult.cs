using System.Collections.Generic;

namespace EmbyKinopoiskTrailers.Api.KinopoiskApiUnofficial.Model
{
    internal sealed class KpSearchResult<TItem>
    {
        public List<TItem> Items { get; set; } = new List<TItem>();
        public int TotalPages { get; set; }
        public int Total { get; set; }
        public bool HasError { get; set; }
    }
}
