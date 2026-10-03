using System.Globalization;

namespace SmsProNikita.Internal;

/// <summary>
/// Формат времени протокола smspro.nikita.kg: YYYYMMDDhhmmss, время бишкекское (GMT+6).
/// </summary>
internal static class SmsProDateTime
{
    private const string Format = "yyyyMMddHHmmss";

    /// <summary>Момент времени в строку протокола (в бишкекском смещении).</summary>
    public static string? ToProtocolString(DateTimeOffset? value) =>
        value?.ToOffset(SmsProTime.BishkekOffset).ToString(Format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Строка протокола в момент времени с бишкекским смещением. Пустая строка (шлюз так
    /// сообщает "ещё не отправлено"/"отчёт не получен") и некорректное значение дают null.
    /// </summary>
    public static DateTimeOffset? FromProtocolString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? new DateTimeOffset(parsed, SmsProTime.BishkekOffset)
            : null;
    }
}
