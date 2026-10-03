namespace SmsProNikita;

/// <summary>
/// Параметры подключения к платформе smspro.nikita.kg.
/// </summary>
/// <remarks>
/// Значения проверяются при старте приложения (<c>ValidateOnStart</c>): пустые
/// <see cref="Login"/>/<see cref="Password"/>, некорректный <see cref="Host"/> или
/// <see cref="Timeout"/> приводят к ошибке конфигурации, а не к ошибке первого запроса.
/// </remarks>
public sealed class SmsProOptions
{
    /// <summary>Секция конфигурации по умолчанию (appsettings.json).</summary>
    public const string SectionName = "SmsPro";

    /// <summary>Login партнёра, выдаваемый при создании аккаунта.</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>Пароль партнёра.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Хост шлюза без схемы. По умолчанию smspro.nikita.kg.</summary>
    public string Host { get; set; } = "smspro.nikita.kg";

    /// <summary>
    /// Использовать HTTPS (рекомендуется, шлюз поддерживает оба протокола).
    /// По умолчанию true.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Таймаут HTTP-запросов. По умолчанию 30 секунд.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Если true — все отправки SMS будут выполняться с флагом &lt;test&gt;1&lt;/test&gt;
    /// (запрос обрабатывается, но реальная отправка и тарификация не производятся,
    /// сервер возвращает <see cref="Models.SmsSendStatus.TestModeNotSent"/>).
    /// Удобно для интеграционных тестов; переопределяется на уровне отдельного запроса.
    /// </summary>
    public bool DefaultTestMode { get; set; }

    internal Uri BaseAddress => new($"{(UseSsl ? "https" : "http")}://{Host}/api/");
}
