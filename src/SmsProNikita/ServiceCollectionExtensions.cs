using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SmsProNikita;

/// <summary>Методы расширения для регистрации <see cref="ISmsProClient"/> в DI-контейнере.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует <see cref="ISmsProClient"/> с настройкой параметров через делегат.
    /// </summary>
    /// <example>
    /// services.AddSmsProNikita(o =>
    /// {
    ///     o.Login = "login";
    ///     o.Password = "passwd";
    /// });
    /// </example>
    public static IServiceCollection AddSmsProNikita(this IServiceCollection services, Action<SmsProOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        AddSmsProNikitaCore(services, services.AddOptions<SmsProOptions>().Configure(configureOptions));
        return services;
    }

    /// <summary>
    /// Регистрирует <see cref="ISmsProClient"/> с настройкой параметров из секции конфигурации
    /// (по умолчанию "SmsPro"), например из appsettings.json.
    /// </summary>
    public static IServiceCollection AddSmsProNikita(this IServiceCollection services, IConfiguration configuration, string sectionName = SmsProOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddSmsProNikitaCore(services, services.AddOptions<SmsProOptions>().Bind(configuration.GetSection(sectionName)));
        return services;
    }

    private static void AddSmsProNikitaCore(IServiceCollection services, OptionsBuilder<SmsProOptions> builder)
    {
        builder
            .Validate(o => !string.IsNullOrWhiteSpace(o.Login), "SmsProOptions.Login обязателен.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Password), "SmsProOptions.Password обязателен.")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Host) && !o.Host.Contains("://", StringComparison.Ordinal),
                "SmsProOptions.Host должен содержать только имя хоста (smspro.nikita.kg), без схемы http:// или https://: за неё отвечает UseSsl.")
            .Validate(
                o => o.Timeout > TimeSpan.Zero && o.Timeout <= TimeSpan.FromMilliseconds(int.MaxValue),
                "SmsProOptions.Timeout должен быть больше нуля и не превышать int.MaxValue миллисекунд.")
            // Ошибки конфигурации выявляются при старте приложения, а не при первом запросе.
            .ValidateOnStart();

        // Логгер нужен и SmsProClient, и обработчикам IHttpClientFactory.
        services.AddLogging();

        // AddHttpClient<ISmsProClient, SmsProClient> регистрирует типизированный клиент
        // поверх пула HttpMessageHandler из IHttpClientFactory.
        services.AddHttpClient<ISmsProClient, SmsProClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SmsProOptions>>().Value;
            client.BaseAddress = options.BaseAddress;
            client.Timeout = options.Timeout;
        });
    }
}
