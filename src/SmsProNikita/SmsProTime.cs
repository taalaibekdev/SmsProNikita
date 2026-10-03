namespace SmsProNikita;

/// <summary>
/// Часовой пояс платформы smspro.nikita.kg: время в протоколе — бишкекское (GMT+6).
/// </summary>
/// <remarks>
/// В Кыргызстане нет перехода на летнее время, поэтому смещение постоянно +06:00 и
/// база часовых поясов не требуется. Шлюз принимает <c>&lt;time&gt;</c> именно в этом поясе,
/// а <c>sendTime</c>/<c>rcvTime</c> в отчётах возвращает в нём же — поэтому клиент
/// приводит все значения к данному смещению.
/// </remarks>
public static class SmsProTime
{
    /// <summary>Смещение времени платформы: GMT+6.</summary>
    public static TimeSpan BishkekOffset { get; } = TimeSpan.FromHours(6);

    /// <summary>Перевести момент времени в бишкекское смещение.</summary>
    public static DateTimeOffset ToBishkek(DateTimeOffset value) => value.ToOffset(BishkekOffset);

    /// <summary>
    /// Прикрепить бишкекское смещение к настенным часам, например "10:00 по Бишкеку".
    /// Удобно для <see cref="Models.SendMessageRequest.ScheduledAt"/>.
    /// </summary>
    /// <param name="wallClock">Время без указания зоны (<see cref="DateTimeKind.Unspecified"/>).</param>
    /// <exception cref="ArgumentException">
    /// Если <paramref name="wallClock"/> помечен как <see cref="DateTimeKind.Utc"/> или
    /// <see cref="DateTimeKind.Local"/>: такое значение уже привязано к другой зоне — сначала
    /// приведите его через <see cref="ToBishkek(DateTimeOffset)"/>.
    /// </exception>
    public static DateTimeOffset FromBishkek(DateTime wallClock)
    {
        if (wallClock.Kind != DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                "Ожидается время без указания зоны (DateTimeKind.Unspecified): бишкекское смещение +06:00 задаёт сам метод.",
                nameof(wallClock));
        }

        return new DateTimeOffset(wallClock, BishkekOffset);
    }

    /// <summary>Текущий момент по Бишкеку. <paramref name="timeProvider"/> позволяет подменить часы в тестах.</summary>
    public static DateTimeOffset GetNowBishkek(TimeProvider? timeProvider = null) =>
        (timeProvider ?? TimeProvider.System).GetUtcNow().ToOffset(BishkekOffset);
}
