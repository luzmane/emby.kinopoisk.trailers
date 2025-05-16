using EmbyKinopoiskTrailers.Api.KinopoiskDev.Model;
using EmbyKinopoiskTrailers.Helper;

using FluentAssertions;

namespace EmbyKinopoiskTrailers.Tests.Common;

public class KpHelperTest
{
    private static readonly DateTimeOffset ExpectedPremierDate = new DateTimeOffset(2024, 1, 25, 12, 34, 56, TimeSpan.Zero)
        .AddMilliseconds(1);

    [Theory]
    [MemberData(nameof(KpPremiereData))]
    public void KpHelper_GetPremierDate(KpPremiere? premiere, string type)
    {
        var date = KpHelper.GetPremierDate(premiere);
        if ("EMPTY".Equals(type, StringComparison.Ordinal) || "NULL".Equals(type, StringComparison.Ordinal))
        {
            date.Should().BeNull();
        }
        else
        {
            date.Should().BeSameDateAs(ExpectedPremierDate, $"'{type}' was defined as '2024-01-25T12:34:56.001Z'");
        }
    }

    public static TheoryData<KpPremiere?, string> KpPremiereData => new()
    {
        {
            new KpPremiere()
            {
                World = "2024-01-25T12:34:56.001Z"
            },
            "World"
        },
        {
            new KpPremiere()
            {
                Russia = "2024-01-25T12:34:56.001Z"
            },
            "Russia"
        },
        {
            new KpPremiere()
            {
                Cinema = "2024-01-25T12:34:56.001Z"
            },
            "Cinema"
        },
        {
            new KpPremiere()
            {
                Digital = "2024-01-25T12:34:56.001Z"
            },
            "Digital"
        },
        {
            new KpPremiere()
            {
                Bluray = "2024-01-25T12:34:56.001Z"
            },
            "Bluray"
        },
        {
            new KpPremiere()
            {
                Dvd = "2024-01-25T12:34:56.001Z"
            },
            "Dvd"
        },
        { new KpPremiere(), "EMPTY" },
        { null, "NULL" }
    };
}
