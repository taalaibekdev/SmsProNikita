namespace SmsProNikita;

/// <summary>
/// Исключение, выбрасываемое при ошибках обмена с платформой smspro.nikita.kg
/// (сетевые ошибки, таймаут, HTTP-код не 2xx, неразбираемый XML-ответ).
/// </summary>
/// <remarks>
/// Бизнес-статусы протокола (например, "недостаточно средств" или "недопустимое имя
/// отправителя") НЕ выбрасываются как исключение — они возвращаются в соответствующем
/// Result-объекте через свойство <c>Status</c>.
/// </remarks>
public sealed class SmsProException : Exception
{
    /// <summary>Создать исключение с описанием ошибки.</summary>
    /// <param name="message">Описание ошибки.</param>
    public SmsProException(string message) : base(message)
    {
    }

    /// <summary>Создать исключение с описанием ошибки и вложенным исключением-причиной.</summary>
    /// <param name="message">Описание ошибки.</param>
    /// <param name="innerException">Исходная ошибка (сетевая, таймаут, разбор XML).</param>
    public SmsProException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
