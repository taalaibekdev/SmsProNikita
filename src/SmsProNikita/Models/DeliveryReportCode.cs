namespace SmsProNikita.Models;

/// <summary>Код отчёта о доставке (поле &lt;report&gt;).</summary>
public enum DeliveryReportCode
{
    /// <summary>Сообщение находится в очереди на отправку.</summary>
    Queued = 0,

    /// <summary>Сообщение отправлено (передано оператору).</summary>
    SentToOperator = 1,

    /// <summary>Сообщение отклонено (не передано).</summary>
    Rejected = 2,

    /// <summary>Сообщение успешно доставлено.</summary>
    Delivered = 3,

    /// <summary>Сообщение не доставлено.</summary>
    NotDelivered = 4,

    /// <summary>Сообщение не отправлено из-за нехватки средств на счету партнера.</summary>
    InsufficientFunds = 5,

    /// <summary>Неизвестный (новый) статус отправки.</summary>
    Unknown = 6,

    /// <summary>Истёк период ожидания отчёта о доставке от SMSC.</summary>
    TimedOut = 7,
}
