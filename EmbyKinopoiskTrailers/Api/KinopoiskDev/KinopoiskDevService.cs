using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using EmbyKinopoiskTrailers.Api.KinopoiskDev.Model;
using EmbyKinopoiskTrailers.Api.Model;
using EmbyKinopoiskTrailers.Helper;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;


namespace EmbyKinopoiskTrailers.Api.KinopoiskDev
{
    internal sealed class KinopoiskDevService : IKinopoiskRuService
    {
        private readonly ILogger _log;
        private readonly KinopoiskDevApi _api;

        internal KinopoiskDevService(
            ILogManager logManager
            , IHttpClient httpClient
            , IJsonSerializer jsonSerializer
            , IActivityManager activityManager
            , INotificationManager notificationManager)
        {
            _log = logManager.GetLogger(GetType().Name);
            _api = new KinopoiskDevApi(logManager, httpClient, jsonSerializer, activityManager, notificationManager);
        }

        public async Task<List<KpLists>> GetKpCollectionsAsync(CancellationToken cancellationToken)
        {
            _log.Info("Fetch Kinopoisk collections");
            KpSearchResult<KpLists> collections = await _api.GetKpCollectionsAsync(cancellationToken);
            if (collections.HasError)
            {
                _log.Info("Failed to fetch Kinopoisk collections");
                return Enumerable.Empty<KpLists>().ToList();
            }

            _log.Info($"Found {collections.Docs.Count} collections");
            return collections.Docs.Where(x => x.MoviesCount > 0).ToList();
        }

        public async Task<List<KpTrailer>> GetTrailersFromCollectionAsync(string collectionId, CancellationToken cancellationToken)
        {
            _log.Info($"Get trailers for '{collectionId}'");
            var toReturn = new HashSet<KpTrailer>(new KpTrailerComparer());
            List<KpMovie> movies = await GetAllCollectionItemsAsync(collectionId, cancellationToken);
            movies.ForEach(m =>
            {
                ProviderIdDictionary providerIdDictionary = new ProviderIdDictionary
                {
                    { Plugin.PluginKey, m.Id.ToString() }
                };
                if (!string.IsNullOrWhiteSpace(m.ExternalId?.Imdb))
                {
                    providerIdDictionary.Add(MetadataProviders.Imdb.ToString(), m.ExternalId.Imdb);
                }

                if (m.ExternalId?.Tmdb != null)
                {
                    providerIdDictionary.Add(MetadataProviders.Tmdb.ToString(), m.ExternalId.Tmdb.ToString());
                }

                m.Videos?.Trailers?.ForEach(t => _ = toReturn.Add(new KpTrailer
                {
                    ImageUrl = m.Poster.PreviewUrl ?? m.Poster.Url,
                    VideoName = m.Name,
                    TrailerName = t.Name,
                    Overview = m.Description,
                    PremierDate = KpHelper.GetPremierDate(m.Premiere),
                    ProviderIds = providerIdDictionary,
                    Url = t.Url
                        .Replace(Constants.YoutubeEmbed, Constants.YoutubeWatch)
                        .Replace(Constants.YoutubeV, Constants.YoutubeWatch),
                }));
                m.Videos?.Teasers?.ForEach(t => _ = toReturn.Add(new KpTrailer
                {
                    ImageUrl = m.Poster.PreviewUrl ?? m.Poster.Url,
                    VideoName = m.Name,
                    TrailerName = t.Name,
                    Overview = m.Description,
                    PremierDate = KpHelper.GetPremierDate(m.Premiere),
                    ProviderIds = providerIdDictionary,
                    Url = t.Url
                        .Replace(Constants.YoutubeEmbed, Constants.YoutubeWatch)
                        .Replace(Constants.YoutubeV, Constants.YoutubeWatch),
                }));
            });
            _log.Info($"Return {toReturn.Count} items for collection '{collectionId}'");

            return toReturn.ToList();
        }

        private async Task<List<KpMovie>> GetAllCollectionItemsAsync(string collectionId, CancellationToken cancellationToken)
        {
            _log.Info($"Get all collection items for '{collectionId}'");
            var movies = new List<KpMovie>();
            // used to get total number of items
            KpSearchResult<KpMovie> tmp = await _api.GetCollectionItemsAsync(collectionId, 1, cancellationToken);
            if (tmp.HasError)
            {
                _log.Error($"Failed to fetch items list from API for collection '{collectionId}'");
                return movies;
            }

            if (tmp.Docs.Count == 0)
            {
                _log.Info($"No items found for collection '{collectionId}'");
                return movies;
            }

            var pages = Math.Ceiling((double)tmp.Total / tmp.Limit);
            movies.AddRange(tmp.Docs);
            for (var i = 2; i <= pages; i++)
            {
                _log.Info($"Fetched page {i} of {pages} pages ({movies.Count} of {tmp.Total} items) for collection '{collectionId}'");
                tmp = await _api.GetCollectionItemsAsync(collectionId, i, cancellationToken);
                if (tmp.HasError)
                {
                    _log.Warn($"Failed to fetch page {i} for collection '{collectionId}");
                    continue;
                }

                movies.AddRange(tmp.Docs);
                _log.Info($"Fetched page {i} of {pages} pages ({movies.Count} of {tmp.Total} items) for collection '{collectionId}'");
            }

            return movies;
        }

    }
}
