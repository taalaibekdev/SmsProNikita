namespace SmsProNikita.Models;

/// <summary>Результат отправки SMS (ответ /api/message).</summary>
public sealed class SendMessageResult
{
    /// <summary>Id запроса, эхо от партнёра.</summary>
    public required string Id { get; init; }

    /// <summary>Статус отправки.</summary>
    public required SmsSendStatus Status { get; init; }

    /// <summary>Число номеров телефонов, распознанное в запросе.</summary>
    public int Phones { get; init; }

    /// <summary>Число частей SMS, на которое разделилось сообщение.</summary>
    public int SmsCount { get; init; }

    /// <summary>Необязательное текстовое описание ошибки от сервера.</summary>
    public string? Message { get; init; }

    /// <summary>true, если Status == Success.</summary>
    public bool IsSuccess => Status == SmsSendStatus.Success;

    /// <summary>
    /// Общее число тарифицируемых SMS = Phones * SmsCount (актуально при успешной отправке).
    /// </summary>
    public int TotalBilledSms => Phones * SmsCount;
}
