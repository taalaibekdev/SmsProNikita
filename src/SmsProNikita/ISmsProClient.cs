using SmsProNikita.Models;

namespace SmsProNikita;

/// <summary>
/// Клиент XML-протокола платформы smspro.nikita.kg для отправки SMS,
/// получения отчётов о доставке, информации об абоненте и состояния счёта.
/// </summary>
/// <remarks>
/// Все методы выполняют один POST-запрос к соответствующему скрипту шлюза
/// (/api/message, /api/dr, /api/def, /api/info). Транспортные и протокольные ошибки
/// выбрасываются как <see cref="SmsProException"/>, бизнес-статусы возвращаются
/// в свойстве <c>Status</c> результата.
/// </remarks>
public interface ISmsProClient
{
    /// <summary>Отправить SMS-сообщение (POST /api/message).</summary>
    /// <remarks>
    /// Если шлюз вернул <see cref="SmsSendStatus.ProcessingTimeout"/>, запрос следует
    /// повторить с ТЕМ ЖЕ <see cref="SendMessageRequest.Id"/> через 5-10 секунд: шлюз
    /// отсекает дубли по id, поэтому повтор не приведёт к двойной отправке.
    /// </remarks>
    /// <param name="request">Параметры отправки; перед вызовом проверяются методом <see cref="SendMessageRequest.Validate"/>.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<SendMessageResult> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить отчёт(ы) о доставке для заданного id сообщения (POST /api/dr).
    /// Если <paramref name="phone"/> не указан — возвращается отчёт по всем номерам транзакции.
    /// </summary>
    /// <param name="id">id сообщения, переданный при отправке.</param>
    /// <param name="phone">Номер телефона в формате 996XXXXXXXXX или +996XXXXXXXXX (необязательно).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<DeliveryReportResult> GetDeliveryReportAsync(string id, string? phone = null, CancellationToken cancellationToken = default);

    /// <summary>Получить регион, оператора и часовой пояс абонента по номеру телефона (POST /api/def).</summary>
    /// <remarks>Информация доступна только по номерам абонентов GSM-сетей Кыргызстана; запрос не тарифицируется.</remarks>
    /// <param name="phone">Номер телефона в формате 996XXXXXXXXX или +996XXXXXXXXX.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<PhoneInfoResult> GetPhoneInfoAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>Получить текущий баланс, цену SMS и статус активности аккаунта (POST /api/info).</summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<AccountInfoResult> GetAccountInfoAsync(CancellationToken cancellationToken = default);
}
