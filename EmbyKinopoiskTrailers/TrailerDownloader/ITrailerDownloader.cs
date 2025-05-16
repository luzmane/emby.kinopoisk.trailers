using System.Threading;
using System.Threading.Tasks;

namespace EmbyKinopoiskTrailers.TrailerDownloader
{
    internal interface ITrailerDownloader
    {
        Task<string> DownloadTrailerByLink(string videoId, string videoName, int preferableQuality, string basePath, CancellationToken cancellationToken);

    }
}
