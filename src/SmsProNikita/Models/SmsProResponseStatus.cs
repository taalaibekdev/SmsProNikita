namespace SmsProNikita.Models;

/// <summary>
/// Общий код статуса ответа, используемый в /api/dr, /api/def и /api/info.
/// Не все значения применимы ко всем методам — см. документацию конкретного метода.
/// </summary>
public enum SmsProResponseStatus
{
    /// <summary>Запрос корректен.</summary>
    Ok = 0,

    /// <summary>Ошибка в формате запроса.</summary>
    FormatError = 1,

    /// <summary>Неверная авторизация (неверный login/pwd).</summary>
    AuthError = 2,

    /// <summary>
    /// Значение 3, смысл зависит от метода: /api/dr и /api/info — недопустимый IP-адрес
    /// отправителя, /api/def — некорректное написание номера телефона.
    /// </summary>
    InvalidIpOrPhone = 3,

    /// <summary>
    /// Значение 4: /api/dr — отчёт для указанных номера телефона и id не найден,
    /// /api/def — данные для номера не найдены в базе. В /api/info код 4 не определён.
    /// </summary>
    NotFound = 4,

    /// <summary>Переполнение очереди запросами с одним и тем же id (только /api/dr).</summary>
    QueueOverflow = 5,
}
