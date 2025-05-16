using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Emby.Notifications;

using EmbyKinopoiskTrailers.Api.KinopoiskDev.Model;
using EmbyKinopoiskTrailers.Api.Model;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace EmbyKinopoiskTrailers.Api.KinopoiskDev
{
    internal class KinopoiskDevApi
    {
        private static readonly IList<string> TrailerPropertiesList = new List<string>
        {
            "alternativeName",
            "externalId",
            "id",
            "name",
            "typeNumber",
            "videos",
            "premiere",
            "description",
            "poster",
        }.AsReadOnly();

        private readonly IHttpClient _httpClient;
        private readonly ILogger _log;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IActivityManager _activityManager;
        private readonly INotificationManager _notificationManager;

        internal KinopoiskDevApi(
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

        internal async Task<KpSearchResult<KpLists>> GetKpCollectionsAsync(CancellationToken cancellationToken)
        {
            var request = "https://api.kinopoisk.dev/v1.4/list?limit=100";
            request += "&selectFields=name&selectFields=slug&selectFields=moviesCount&selectFields=cover&selectFields=category";
            var json = await SendRequestAsync(request, cancellationToken);
            var hasError = json.Length == 0;
            return hasError
                ? new KpSearchResult<KpLists>
                {
                    HasError = true
                }
                : _jsonSerializer.DeserializeFromString<KpSearchResult<KpLists>>(json);
        }

        internal async Task<KpSearchResult<KpMovie>> GetCollectionItemsAsync(string collectionId, int page, CancellationToken cancellationToken)
        {
            var request = new StringBuilder($"https://api.kinopoisk.dev/v1.4/movie?limit=250&page={page}&lists={collectionId}")
                .Append($"&selectFields={string.Join("&selectFields=", TrailerPropertiesList)}")
                .ToString();
            var json = await SendRequestAsync(request, cancellationToken);
            var hasError = json.Length == 0;
            return hasError
                ? new KpSearchResult<KpMovie>
                {
                    HasError = true
                }
                : _jsonSerializer.DeserializeFromString<KpSearchResult<KpMovie>>(json);
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
            options.RequestHeaders.Add("accept", "application/json");
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
                            case 400:
                                var error = _jsonSerializer.DeserializeFromString<KpErrorResponse>(result);
                                var msg = $"{error.Error}: {error.Message.FirstOrDefault()} for URL: '{url}'";
                                _log.Error(msg);
                                return string.Empty;
                            case 401:
                                msg = $"Token is invalid: '{token}'";
                                _log.Error(msg);
                                NotifyUser(msg, "Token is invalid");
                                return string.Empty;
                            case 403:
                                msg = "Request limit exceeded (either daily or total) for current token";
                                _log.Warn(msg);
                                NotifyUser(msg, "Request limit exceeded");
                                return string.Empty;
                            default:
                                error = _jsonSerializer.DeserializeFromString<KpErrorResponse>(result);
                                msg = $"Received '{response.StatusCode}' from API";
                                msg += error == null
                                    ? $": '{result}' for URL: '{url}'"
                                    : $" - Error:'{error.Error}', Message:'{error.Message.FirstOrDefault()}' for URL: '{url}'";
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
                    case 403:
                        msg = $"Request limit exceeded (either daily or total) for current token.{(string.IsNullOrWhiteSpace(content) ? string.Empty : " Message: '" + content + "'.")} For '{url}'";
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
