namespace SmsProNikita.Models;

/// <summary>Результат запроса информации о состоянии счёта и аккаунта (ответ /api/info).</summary>
public sealed class AccountInfoResult
{
    /// <summary>Статус запроса.</summary>
    public required SmsProResponseStatus Status { get; init; }

    /// <summary>true — аккаунт активен, false — не активен либо заблокирован.</summary>
    public bool IsActive { get; init; }

    /// <summary>Состояние счёта в валюте счёта.</summary>
    public decimal Account { get; init; }

    /// <summary>Стоимость одного SMS-сообщения в валюте счёта.</summary>
    public decimal SmsPrice { get; init; }

    /// <summary>true, если Status == Ok.</summary>
    public bool IsSuccess => Status == SmsProResponseStatus.Ok;
}
