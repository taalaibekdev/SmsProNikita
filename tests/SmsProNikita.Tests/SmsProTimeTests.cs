using SmsProNikita.Internal;

namespace SmsProNikita.Tests;

public sealed class SmsProTimeTests
{
    [Fact]
    public void BishkekOffsetIsSixHours()
    {
        Assert.Equal(TimeSpan.FromHours(6), SmsProTime.BishkekOffset);
    }

    [Fact]
    public void ToBishkekConvertsUtcMoment()
    {
        var utc = new DateTimeOffset(2026, 1, 2, 20, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateTimeOffset(2026, 1, 3, 2, 0, 0, SmsProTime.BishkekOffset), SmsProTime.ToBishkek(utc));
    }

    [Fact]
    public void FromBishkekAttachesOffsetToWallClock()
    {
        var scheduled = SmsProTime.FromBishkek(new DateTime(2026, 1, 3, 10, 0, 0));

        Assert.Equal(TimeSpan.FromHours(6), scheduled.Offset);
        Assert.Equal(new TimeSpan(10, 0, 0), scheduled.TimeOfDay);
    }

    [Fact]
    public void FromBishkekRejectsZonedDateTime()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SmsProTime.FromBishkek(new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc)));

        Assert.Equal("wallClock", exception.ParamName);
    }

    [Fact]
    public void GetNowBishkekUsesProvidedTimeProvider()
    {
        var utcNow = new DateTimeOffset(2026, 1, 2, 20, 0, 0, TimeSpan.Zero);
        var timeProvider = new FixedTimeProvider(utcNow);

        Assert.Equal(new DateTimeOffset(2026, 1, 3, 2, 0, 0, SmsProTime.BishkekOffset), SmsProTime.GetNowBishkek(timeProvider));
    }

    [Fact]
    public void ProtocolTimeConvertsToBishkek()
    {
        // 2026-01-02 20:00:00 UTC == 2026-01-03 02:00:00 по Бишкеку.
        Assert.Equal("20260103020000", SmsProDateTime.ToProtocolString(new DateTimeOffset(2026, 1, 2, 20, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void ProtocolTimeIsNullWhenNotScheduled()
    {
        Assert.Null(SmsProDateTime.ToProtocolString(null));
    }

    [Theory]
    [InlineData("20100921235957", 2010, 9, 21, 23, 59, 57)]
    [InlineData("20260103020000", 2026, 1, 3, 2, 0, 0)]
    public void ProtocolTimeParsesWithBishkekOffset(string value, int year, int month, int day, int hour, int minute, int second)
    {
        var expected = new DateTimeOffset(year, month, day, hour, minute, second, SmsProTime.BishkekOffset);

        Assert.Equal(expected, SmsProDateTime.FromProtocolString(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("не-дата")]
    [InlineData("2010092123595")]
    public void ProtocolTimeIsNullForEmptyOrInvalidValue(string? value)
    {
        Assert.Null(SmsProDateTime.FromProtocolString(value));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
