namespace SmsProNikita.Models;

/// <summary>
/// Одна транзакция в push-отчёте, присылаемом шлюзом на URL партнёра (раздел 2.1 протокола).
/// </summary>
public sealed class DeliveryReportPushEntry
{
    /// <summary>id сообщения, для которого производилась отправка (тот же id, что в запросе /api/message).</summary>
    public required string Id { get; init; }

    /// <summary>Отчёты по номерам телефонов в этой транзакции.</summary>
    public IReadOnlyList<PhoneDeliveryReport> Phones { get; init; } = [];
}

/// <summary>
/// Разобранный push-отчёт о доставке, полученный на URL партнёра, настроенный
/// в личном кабинете (МОЙ ПРОФИЛЬ -&gt; ПАРАМЕТРЫ API -&gt; "Включить отправку отчётов").
/// </summary>
/// <remarks>
/// Один XML-отчёт может содержать несколько транзакций (<c>dr</c>), в каждой — несколько
/// номеров (<c>phone</c>). В ответ на приём отчёта ваш скрипт обязан вернуть HTTP 200 OK,
/// иначе шлюз повторит доставку 3 раза с интервалом 10 минут.
/// </remarks>
public sealed class DeliveryReportPush
{
    /// <summary>Список транзакций, включённых в отчёт.</summary>
    public IReadOnlyList<DeliveryReportPushEntry> Entries { get; init; } = [];
}
