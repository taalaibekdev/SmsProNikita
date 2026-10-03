namespace SmsProNikita.Models;

/// <summary>Статус ответа на запрос отправки SMS (поле &lt;status&gt; в /api/message).</summary>
public enum SmsSendStatus
{
    /// <summary>Сообщения успешно приняты к отправке.</summary>
    Success = 0,

    /// <summary>Ошибка в формате запроса.</summary>
    FormatError = 1,

    /// <summary>Неверная авторизация.</summary>
    AuthError = 2,

    /// <summary>Недопустимый IP-адрес отправителя.</summary>
    InvalidIpAddress = 3,

    /// <summary>Недостаточно средств на счету клиента.</summary>
    InsufficientFunds = 4,

    /// <summary>Недопустимое имя отправителя (sender не провалидирован администратором).</summary>
    InvalidSenderName = 5,

    /// <summary>Сообщение заблокировано по стоп-словам.</summary>
    BlockedByStopWords = 6,

    /// <summary>Некорректное написание одного или нескольких номеров телефонов.</summary>
    InvalidPhoneNumber = 7,

    /// <summary>Неверный формат времени отправки.</summary>
    InvalidTimeFormat = 8,

    /// <summary>Превышение времени обработки запроса. Повторите с тем же id через 5-10 сек.</summary>
    ProcessingTimeout = 9,

    /// <summary>Отправка заблокирована из-за последовательного повторения id.</summary>
    DuplicateId = 10,

    /// <summary>Сообщение обработано, но не отправлено и не тарифицировано (test=1).</summary>
    TestModeNotSent = 11,
}
