using System;
using System.Globalization;

using EmbyKinopoiskTrailers.Api.KinopoiskDev.Model;

namespace EmbyKinopoiskTrailers.Helper
{
    internal static class KpHelper
    {
        internal const string PremierDateFormat = "yyyy-MM-dd'T'HH:mm:ss.fffZ";

        internal static DateTimeOffset? GetPremierDate(KpPremiere premiere)
        {
            if (premiere == null)
            {
                return null;
            }

            if (DateTimeOffset.TryParseExact(
                    premiere.World,
                    PremierDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset world))
            {
                return world;
            }

            if (DateTimeOffset.TryParseExact(
                    premiere.Russia,
                    PremierDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset russia))
            {
                return russia;
            }

            if (DateTimeOffset.TryParseExact(
                    premiere.Cinema,
                    PremierDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset cinema))
            {
                return cinema;
            }

            if (DateTimeOffset.TryParseExact(
                    premiere.Digital,
                    PremierDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset digital))
            {
                return digital;
            }

            if (DateTimeOffset.TryParseExact(
                    premiere.Bluray,
                    PremierDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset bluray))
            {
                return bluray;
            }

            if (DateTimeOffset.TryParseExact(
                    premiere.Dvd,
                    PremierDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset dvd))
            {
                return dvd;
            }

            return null;
        }
    }
}
