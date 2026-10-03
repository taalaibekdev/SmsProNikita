namespace SmsProNikita.Tests;

public sealed class MessageIdTests
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    [Fact]
    public void NewRandomHasProtocolShape()
    {
        for (var i = 0; i < 1_000; i++)
        {
            var id = MessageId.NewRandom();

            Assert.Equal(MessageId.Length, id.Length);
            Assert.All(id, c => Assert.Contains(c, Alphabet));
        }
    }

    [Fact]
    public void NewHasProtocolShape()
    {
        for (var i = 0; i < 1_000; i++)
        {
            var id = MessageId.New();

            Assert.Equal(MessageId.Length, id.Length);
            Assert.All(id, c => Assert.Contains(c, Alphabet));
        }
    }

    [Fact]
    public void NewRandomIsUnique()
    {
        // 10 000 значений в пространстве 36^12 ≈ 4,7·10^18: ожидаемая вероятность коллизии ~10^-11.
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < 10_000; i++)
            Assert.True(ids.Add(MessageId.NewRandom()), "NewRandom выдал повтор");
    }

    [Fact]
    public void NewIsUniqueWithinMinute()
    {
        // В пределах минуты доступно 36^6 ≈ 2,18 млрд случайных значений,
        // поэтому 2 000 id дают вероятность коллизии меньше 0,1 %.
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < 2_000; i++)
            Assert.True(ids.Add(MessageId.New()), "New выдал повтор");
    }

    [Fact]
    public void NewStartsWithMinuteInBase36()
    {
        var id = MessageId.New();
        var expected = ToBase36(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60).PadLeft(6, '0');

        // Допускаем переход на следующую минуту между генерацией id и проверкой.
        var previous = ToBase36((DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60) - 1).PadLeft(6, '0');

        Assert.True(
            id.StartsWith(expected, StringComparison.Ordinal) || id.StartsWith(previous, StringComparison.Ordinal),
            $"Префикс {id[..6]} не соответствует текущей минуте {expected} (или предыдущей {previous}).");
    }

    [Fact]
    public void NewIdsAreChronologicallyOrdered()
    {
        var first = MessageId.New()[..6];
        var second = MessageId.New()[..6];

        Assert.True(string.CompareOrdinal(first, second) <= 0);
    }

    private static string ToBase36(long value)
    {
        var result = string.Empty;

        do
        {
            result = Alphabet[(int)(value % 36)] + result;
            value /= 36;
        }
        while (value > 0);

        return result;
    }
}
