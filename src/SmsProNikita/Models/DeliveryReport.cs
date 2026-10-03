namespace SmsProNikita.Models;

/// <summary>Отчёт о доставке для одного номера телефона.</summary>
public sealed class PhoneDeliveryReport
{
    /// <summary>Номер телефона получателя.</summary>
    public required string Number { get; init; }

    /// <summary>Код отчёта о доставке.</summary>
    public required DeliveryReportCode Report { get; init; }

    /// <summary>
    /// Время фактической отправки оператору (бишкекское смещение, GMT+6).
    /// null — сообщение ещё не отправлялось.
    /// </summary>
    public DateTimeOffset? SendTime { get; init; }

    /// <summary>
    /// Время получения отчёта о доставке (бишкекское смещение, GMT+6).
    /// null — отчёт ещё не получен.
    /// </summary>
    public DateTimeOffset? ReceiveTime { get; init; }
}

/// <summary>Результат запроса отчёта о доставке (ответ /api/dr).</summary>
public sealed class DeliveryReportResult
{
    /// <summary>Статус запроса.</summary>
    public required SmsProResponseStatus Status { get; init; }

    /// <summary>Отчёты по каждому запрошенному номеру телефона.</summary>
    public IReadOnlyList<PhoneDeliveryReport> Phones { get; init; } = [];

    /// <summary>true, если Status == Ok.</summary>
    public bool IsSuccess => Status == SmsProResponseStatus.Ok;
}
