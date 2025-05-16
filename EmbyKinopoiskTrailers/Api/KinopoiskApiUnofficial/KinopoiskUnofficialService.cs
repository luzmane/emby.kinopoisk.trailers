using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using EmbyKinopoiskTrailers.Api.KinopoiskApiUnofficial.Model;
using EmbyKinopoiskTrailers.Api.Model;
using EmbyKinopoiskTrailers.Helper;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyKinopoiskTrailers.Api.KinopoiskApiUnofficial
{
    internal sealed class KinopoiskUnofficialService : IKinopoiskRuService
    {
        private readonly ILogger _log;
        private readonly KinopoiskUnofficialApi _api;

        private static readonly List<KpLists> KpCollections = new List<KpLists>
        {
            new KpLists
            {
                Slug = "the_closest_releases",
                Name = "Ближайшие премьеры",
                Category = "Онлайн-кинотеатр"
            },
            new KpLists
            {
                Slug = "theme_comics",
                Name = "Лучшие фильмы, основанные на комиксах",
                Category = "Фильмы"
            },
            new KpLists
            {
                Slug = "theme_catastrophe",
                Name = "Фильмы-катастрофы",
                Category = "Фильмы"
            },
            new KpLists
            {
                Slug = "hd-family",
                Name = "Смотрим всей семьей",
                Category = "Онлайн-кинотеатр"
            },
            new KpLists
            {
                Slug = "theme_kids_animation",
                Name = "Мультфильмы для самых маленьких",
                Category = "Фильмы"
            },
            new KpLists
            {
                Slug = "theme_love",
                Name = "Фильмы про любовь и страсть: список лучших романтических фильмов",
                Category = "Фильмы"
            },
            new KpLists
            {
                Slug = "oscar_winners_2021",
                Name = "«Оскар-2021»: победители",
                Category = "Премии"
            },
            new KpLists
            {
                Slug = "series-top250",
                Name = "250 лучших сериалов",
                Category = "Сериалы"
            },
            new KpLists
            {
                Slug = "top250",
                Name = "250 лучших фильмов",
                Category = "Фильмы"
            },
            new KpLists
            {
                Slug = "popular-series",
                Name = "Популярные сериалы",
                Category = "Сериалы"
            },
            new KpLists
            {
                Slug = "popular-films",
                Name = "Популярные фильмы",
                Category = "Фильмы"
            },
            new KpLists
            {
                Slug = "theme_vampire",
                Name = "Фильмы про вампиров",
                Category = "Фильмы"
            },
            new KpLists
            {
                Slug = "theme_zombie",
                Name = "Фильмы про зомби: список лучших фильмов про живых мертвецов",
                Category = "Фильмы"
            }
        };

        private static readonly Dictionary<string, string> CollectionSlugMap = new Dictionary<string, string>
        {
            { "theme_comics", "COMICS_THEME" },
            { "series-top250", "TOP_250_TV_SHOWS" },
            { "top250", "TOP_250_MOVIES" },
            { "popular-series", "TOP_POPULAR_ALL" },
            { "popular-films", "TOP_POPULAR_MOVIES" },
            { "theme_vampire", "VAMPIRE_THEME" },
        };

        internal KinopoiskUnofficialService(
            ILogManager logManager
            , IHttpClient httpClient
            , IJsonSerializer jsonSerializer
            , IActivityManager activityManager
            , INotificationManager notificationManager)
        {
            _log = logManager.GetLogger(GetType().Name);
            _api = new KinopoiskUnofficialApi(logManager, httpClient, jsonSerializer, activityManager, notificationManager);
        }

        public Task<List<KpLists>> GetKpCollectionsAsync(CancellationToken cancellationToken)
        {
            _log.Info("KinopoiskUnofficial doesn't have method to fetch collection, so list is hardcoded");
            return Task.FromResult(KpCollections);
        }

        public async Task<List<KpTrailer>> GetTrailersFromCollectionAsync(string collectionId, CancellationToken cancellationToken)
        {
            _log.Info($"Get trailers for '{collectionId}'");
            var toReturn = new HashSet<KpTrailer>(new KpTrailerComparer());
            if (!CollectionSlugMap.TryGetValue(collectionId, out var slug))
            {
                _log.Error($"Unable to map '{collectionId}' to collection id of kinopoiskapiunofficial.tech");
                return toReturn.ToList();
            }

            _log.Info($"'{collectionId}' mapped to '{slug}'");
            List<KpFilm> movies = await GetAllCollectionItemsAsync(slug, cancellationToken);
            foreach (var movie in movies)
            {
                ProviderIdDictionary providerIdDictionary = new ProviderIdDictionary
                {
                    { Plugin.PluginKey, movie.KinopoiskId.ToString() }
                };
                if (!string.IsNullOrWhiteSpace(movie.ImdbId))
                {
                    providerIdDictionary.Add(MetadataProviders.Imdb.ToString(), movie.ImdbId);
                }

                var tmp = await _api.GetVideosByFilmIdAsync(movie.KinopoiskId.ToString(), cancellationToken);
                if (tmp.HasError)
                {
                    _log.Error($"Failed to fetch trailers from API for videoId '{movie.KinopoiskId}'");
                }
                else
                {
                    _log.Debug($"Video with Id '{movie.KinopoiskId}' has '{tmp.Items.Count}' trailers");
                    tmp.Items.ForEach(t => toReturn.Add(new KpTrailer
                    {
                        ImageUrl = string.IsNullOrWhiteSpace(movie.PosterUrlPreview) ? movie.PosterUrl : movie.PosterUrlPreview,
                        VideoName = movie.NameRu,
                        TrailerName = t.Name,
                        Overview = movie.Description,
                        ProviderIds = providerIdDictionary,
                        Url = t.Url,
                        PremierDate = movie.Year > 1000 && movie.Year < 3000
                            ? new DateTimeOffset((int)movie.Year, 1, 1, 0, 0, 0, TimeSpan.Zero)
                            : (DateTimeOffset?)null
                    }));
                }
            }

            _log.Info($"Return {toReturn.Count} items for collection '{collectionId}'");

            return toReturn.ToList();
        }

        private async Task<List<KpFilm>> GetAllCollectionItemsAsync(string collectionId, CancellationToken cancellationToken)
        {
            _log.Info($"Get all collection items for '{collectionId}'");
            var movies = new List<KpFilm>();
            KpSearchResult<KpFilm> tmp = await _api.GetCollectionItemsAsync(collectionId, 1, cancellationToken);
            if (tmp.HasError)
            {
                _log.Error($"Failed to fetch items list from API for collection '{collectionId}'");
                return movies;
            }

            if (tmp.Items.Count == 0)
            {
                _log.Info($"No items found for collection '{collectionId}'");
                return movies;
            }

            movies.AddRange(tmp.Items);
            for (var i = 2; i <= tmp.TotalPages; i++)
            {
                _log.Info($"Requesting page {i} of {tmp.TotalPages} pages ({movies.Count} of {tmp.Total} items) for collection '{collectionId}'");
                tmp = await _api.GetCollectionItemsAsync(collectionId, i, cancellationToken);
                if (tmp.HasError)
                {
                    _log.Warn($"Failed to fetch page {i} for collection '{collectionId}");
                    continue;
                }

                movies.AddRange(tmp.Items);
                _log.Info($"Fetched page {i} of {tmp.TotalPages} pages ({movies.Count} of {tmp.Total} items) for collection '{collectionId}'");
            }

            return movies;
        }
    }
}
