using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Emby.Notifications;

using EmbyKinopoiskTrailers.Api.KinopoiskApiUnofficial.Model;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace EmbyKinopoiskTrailers.Api.KinopoiskApiUnofficial
{
    internal class KinopoiskUnofficialApi
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _log;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IActivityManager _activityManager;
        private readonly INotificationManager _notificationManager;

        internal KinopoiskUnofficialApi(
            ILogManager logManager
            , IHttpClient httpClient
            , IJsonSerializer jsonSerializer
            , IActivityManager activityManager
            , INotificationManager notificationManager)
        {
            _httpClient = httpClient;
            _log = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
            _activityManager = activityManager;
            _notificationManager = notificationManager;
        }

        internal async Task<KpSearchResult<KpFilm>> GetCollectionItemsAsync(string collectionId, int page, CancellationToken cancellationToken)
        {
            var request = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/collections?page={page}&type={collectionId}";
            var response = await SendRequestAsync(request, cancellationToken);
            return _jsonSerializer.DeserializeFromString<KpSearchResult<KpFilm>>(response)
                   ?? new KpSearchResult<KpFilm>
                   {
                       HasError = true
                   };
        }

        internal async Task<KpSearchResult<KpVideo>> GetVideosByFilmIdAsync(string movieId, CancellationToken cancellationToken)
        {
            var url = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/{movieId}/videos";
            var response = await SendRequestAsync(url, cancellationToken);
            return _jsonSerializer.DeserializeFromString<KpSearchResult<KpVideo>>(response)
                   ?? new KpSearchResult<KpVideo>
                   {
                       HasError = true
                   };
        }

        private async Task<string> SendRequestAsync(string url, CancellationToken cancellationToken)
        {
            _log.Debug($"Sending request to {url}");
            var token = Plugin.Instance?.Configuration.GetCurrentToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                _log.Error("The token is empty. Skip request");
                return string.Empty;
            }

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                CacheLength = TimeSpan.FromHours(12),
                CacheMode = CacheMode.Unconditional,
                TimeoutMs = 30_000,
                DecompressionMethod = CompressionMethod.Gzip,
                EnableHttpCompression = true,
                EnableDefaultUserAgent = true
            };
            options.RequestHeaders.Add("X-API-KEY", token);
            options.Sanitation.SanitizeDefaultParams = false;

            HttpResponseInfo response = null;
            try
            {
                using (response = await _httpClient.GetResponse(options))
                {
                    using (var reader = new StreamReader(response.Content))
                    {
                        var result = await reader.ReadToEndAsync();
                        switch ((int)response.StatusCode)
                        {
                            case int n when n >= 200 && n < 300:
                                _log.Debug($"Received response: '{result}'");
                                return result;
                            case 401:
                                var msg = $"Token is invalid: '{token}'";
                                _log.Error(msg);
                                NotifyUser(msg, "Token is invalid");
                                return string.Empty;
                            case 402:
                                msg = $"Request limit exceeded (either daily or total) for current token.{(string.IsNullOrWhiteSpace(result) ? string.Empty : " Message: '" + result + "'.")} For '{url}'";
                                _log.Warn(msg);
                                NotifyUser(msg, "Request limit exceeded");
                                return string.Empty;
                            case 404:
                                _log.Info($"Data not found for URL: '{url}'");
                                return string.Empty;
                            case 429:
                                _log.Info("Too many requests per second. Waiting 2 sec");
                                await Task.Delay(2000, cancellationToken);
                                return await SendRequestAsync(url, cancellationToken);
                            default:
                                msg = $"Received '{response.StatusCode}' from API: '{result}' for URL: '{url}'";
                                _log.Error(msg);
                                return string.Empty;
                        }
                    }
                }
            }
            catch (HttpException ex)
            {
                var content = string.Empty;
                if (response?.Content != null)
                {
                    using (var reader = new StreamReader(response.Content))
                    {
                        content = await reader.ReadToEndAsync();
                    }
                }

                switch ((int?)ex.StatusCode)
                {
                    case 401:
                        var msg = $"Token is invalid: '{token}'.{(string.IsNullOrWhiteSpace(content) ? string.Empty : " Message: '" + content + "'")}";
                        _log.Error(msg);
                        NotifyUser(msg, "Token is invalid");
                        break;
                    case 402:
                        msg = $"Request limit exceeded (either daily or total) for current token.{(string.IsNullOrWhiteSpace(content) ? string.Empty : " Message: '" + content + "'")}";
                        _log.Warn(msg);
                        NotifyUser(msg, "Request limit exceeded");
                        break;
                    default:
                        msg = $"Received '{ex.StatusCode}' from API: '{(string.IsNullOrWhiteSpace(content) ? ex.Message : content)}'";
                        _log.Error(msg, ex);
                        break;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                var msg = $"Unable to fetch data from URL '{url}' due to {ex.Message}";
                _log.ErrorException(msg, ex);
                return string.Empty;
            }
        }

        private void NotifyUser(string overview, string shortOverview)
        {
            _activityManager.Create(new ActivityLogEntry
            {
                Name = Plugin.PluginKey,
                Type = "PluginError",
                Overview = overview,
                ShortOverview = shortOverview,
                Severity = LogSeverity.Error
            });

            _notificationManager.SendNotification(new NotificationRequest
            {
                Description = overview,
                Title = shortOverview
            });
        }
    }
}
