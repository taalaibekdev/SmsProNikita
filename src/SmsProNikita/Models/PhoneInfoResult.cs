namespace SmsProNikita.Models;

/// <summary>Результат запроса информации об абоненте по номеру телефона (ответ /api/def).</summary>
public sealed class PhoneInfoResult
{
    /// <summary>Статус запроса.</summary>
    public required SmsProResponseStatus Status { get; init; }

    /// <summary>Регион (например, "Бишкек,Бишкек"). Заполнено только при Status == Ok.</summary>
    public string? Region { get; init; }

    /// <summary>Сотовый оператор (например, "MegaCom"). Заполнено только при Status == Ok.</summary>
    public string? Operator { get; init; }

    /// <summary>GMT-смещение часового пояса абонента. -1, если неизвестно.</summary>
    public int Timezone { get; init; } = -1;

    /// <summary>true, если Status == Ok.</summary>
    public bool IsSuccess => Status == SmsProResponseStatus.Ok;
}
