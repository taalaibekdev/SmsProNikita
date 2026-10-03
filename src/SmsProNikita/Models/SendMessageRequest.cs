using SmsProNikita.Internal;

namespace SmsProNikita.Models;

/// <summary>Запрос на отправку SMS-сообщения через /api/message.</summary>
public sealed class SendMessageRequest
{
    /// <summary>
    /// Уникальный id сообщения: латинские буквы и цифры, до 12 знаков.
    /// Используется для защиты от дублей и для получения отчётов о доставке.
    /// Один id — одно сообщение (один пакет до 50 номеров). Для повторной отправки
    /// того же текста нужен новый id, а при повторе после таймаута — тот же самый.
    /// Готовый генератор: <see cref="MessageId.New"/>.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Имя отправителя, отображаемое в телефоне получателя: до 11 латинских букв, цифр,
    /// точек и тире, либо 14 цифр. Имя должно быть согласовано с администратором шлюза,
    /// иначе сервер вернёт <see cref="SmsSendStatus.InvalidSenderName"/>.
    /// </summary>
    public required string Sender { get; set; }

    /// <summary>Текст сообщения, до 800 символов, UTF-8.</summary>
    public required string Text { get; set; }

    /// <summary>
    /// Номера получателей в формате 996XXXXXXXXX или +996XXXXXXXXX. До 50 номеров в одном пакете.
    /// </summary>
    public required IReadOnlyCollection<string> Phones { get; set; }

    /// <summary>
    /// Время отложенной отправки. Протокол передаёт время в бишкекском поясе (GMT+6), клиент
    /// приводит значение к этому поясу сам — см. <see cref="SmsProTime"/>. Если null —
    /// сообщение отправляется немедленно.
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; set; }

    /// <summary>
    /// Тестовый режим: запрос обрабатывается сервером, но реальная отправка
    /// и тарификация не производятся (сервер вернёт <see cref="SmsSendStatus.TestModeNotSent"/>).
    /// </summary>
    public bool Test { get; set; }

    /// <summary>Базовая валидация значений перед отправкой запроса.</summary>
    /// <exception cref="ArgumentException">Если какое-либо поле не соответствует формату протокола.</exception>
    public void Validate()
    {
        // char.IsLetterOrDigit пропустил бы кириллицу («АВС123»), которую шлюз не примет.
        if (string.IsNullOrWhiteSpace(Id) || Id.Length > 12 || !Id.All(IsLatinLetterOrDigit))
            throw new ArgumentException("Id должен состоять из латинских букв и цифр (A-Z, a-z, 0-9), длина до 12 символов.", nameof(Id));

        if (string.IsNullOrWhiteSpace(Sender)
            || (!ProtocolRules.AlphanumericSender().IsMatch(Sender) && !ProtocolRules.NumericSender().IsMatch(Sender)))
        {
            throw new ArgumentException(
                "Sender должен состоять из 1-11 латинских букв, цифр, точек или тире, либо из 14 цифр.",
                nameof(Sender));
        }

        if (string.IsNullOrEmpty(Text) || Text.Length > 800)
            throw new ArgumentException("Text не может быть пустым и не должен превышать 800 символов.", nameof(Text));

        if (Phones is null || Phones.Count == 0)
            throw new ArgumentException("Нужно указать хотя бы один номер телефона.", nameof(Phones));

        if (Phones.Count > 50)
            throw new ArgumentException("Максимальное число получателей в одном пакете — 50.", nameof(Phones));

        foreach (var phone in Phones)
        {
            if (string.IsNullOrWhiteSpace(phone) || !ProtocolRules.PhoneNumber().IsMatch(phone))
            {
                throw new ArgumentException(
                    $"Номер «{phone}» не соответствует формату протокола: 996XXXXXXXXX или +996XXXXXXXXX.",
                    nameof(Phones));
            }
        }
    }

    private static bool IsLatinLetterOrDigit(char c) =>
        c is >= '0' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z';
}
