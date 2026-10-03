using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmsProNikita.Internal;
using SmsProNikita.Models;

namespace SmsProNikita;

/// <inheritdoc cref="ISmsProClient" />
public sealed partial class SmsProClient : ISmsProClient
{
    private readonly HttpClient _httpClient;
    private readonly SmsProOptions _options;
    private readonly ILogger<SmsProClient> _logger;

    /// <summary>
    /// Создать клиент. Обычно экземпляр создаёт DI-контейнер через <c>AddSmsProNikita</c>,
    /// который подставляет настроенный <see cref="HttpClient"/> и логгер.
    /// </summary>
    /// <param name="httpClient">HTTP-клиент с заполненным <see cref="HttpClient.BaseAddress"/>.</param>
    /// <param name="options">Параметры подключения (login, пароль, хост, таймаут).</param>
    /// <param name="logger">Логгер для диагностики обмена с шлюзом.</param>
    public SmsProClient(HttpClient httpClient, IOptions<SmsProOptions> options, ILogger<SmsProClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SendMessageResult> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var test = request.Test || _options.DefaultTestMode;

        var requestXml = ProtocolXml.BuildMessageRequest(
            _options.Login,
            _options.Password,
            request.Id,
            request.Sender,
            request.Text,
            SmsProDateTime.ToProtocolString(request.ScheduledAt),
            request.Phones,
            test);

        LogSendingMessage(_logger, request.Id, request.Phones.Count, test);

        var responseXml = await PostAsync("message", requestXml, cancellationToken).ConfigureAwait(false);
        var result = ProtocolXml.ParseMessageResponse(responseXml);

        if (result.IsSuccess)
            LogSendResult(_logger, result.Id, result.Status, result.Phones, result.SmsCount);
        else
            LogSendFailed(_logger, result.Id, result.Status, result.Message);

        return result;
    }

    /// <inheritdoc />
    public async Task<DeliveryReportResult> GetDeliveryReportAsync(string id, string? phone = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        LogDeliveryReportRequest(_logger, id, phone);

        var requestXml = ProtocolXml.BuildDrRequest(_options.Login, _options.Password, id, phone);
        var responseXml = await PostAsync("dr", requestXml, cancellationToken).ConfigureAwait(false);

        return ProtocolXml.ParseDrResponse(responseXml);
    }

    /// <inheritdoc />
    public async Task<PhoneInfoResult> GetPhoneInfoAsync(string phone, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        var requestXml = ProtocolXml.BuildDefRequest(_options.Login, _options.Password, phone);
        var responseXml = await PostAsync("def", requestXml, cancellationToken).ConfigureAwait(false);

        return ProtocolXml.ParseDefResponse(responseXml);
    }

    /// <inheritdoc />
    public async Task<AccountInfoResult> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        var requestXml = ProtocolXml.BuildInfoRequest(_options.Login, _options.Password);
        var responseXml = await PostAsync("info", requestXml, cancellationToken).ConfigureAwait(false);

        return ProtocolXml.ParseInfoResponse(responseXml);
    }

    private async Task<string> PostAsync(string relativeUrl, string requestXml, CancellationToken cancellationToken)
    {
        // StringContent sets Content-Type to "application/xml; charset=utf-8" from the encoding + media type below.
        using var content = new StringContent(requestXml, Encoding.UTF8, "application/xml");

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await _httpClient.PostAsync(relativeUrl, content, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogTransportError(_logger, relativeUrl, ex);
            throw new SmsProException($"Ошибка HTTP-запроса к smspro.nikita.kg ({relativeUrl}): {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogTimeout(_logger, relativeUrl, _options.Timeout, ex);
            throw new SmsProException($"Таймаут запроса к smspro.nikita.kg ({relativeUrl}).", ex);
        }

        using (httpResponse)
        {
            var responseXml = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                LogHttpError(_logger, (int)httpResponse.StatusCode, relativeUrl, responseXml);
                throw new SmsProException(
                    $"smspro.nikita.kg вернул HTTP {(int)httpResponse.StatusCode} для {relativeUrl}. Тело ответа: {responseXml}");
            }

            return responseXml;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Отправка SMS: id={Id}, получателей={PhoneCount}, тестовый режим={Test}")]
    private static partial void LogSendingMessage(ILogger logger, string id, int phoneCount, bool test);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "SMS приняты шлюзом: id={Id}, status={Status}, номеров={Phones}, частей SMS={SmsCount}")]
    private static partial void LogSendResult(ILogger logger, string id, SmsSendStatus status, int phones, int smsCount);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Шлюз отклонил отправку: id={Id}, status={Status}, message={Message}")]
    private static partial void LogSendFailed(ILogger logger, string id, SmsSendStatus status, string? message);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug,
        Message = "Запрос отчёта о доставке: id={Id}, номер={Phone}")]
    private static partial void LogDeliveryReportRequest(ILogger logger, string id, string? phone);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
        Message = "Шлюз вернул HTTP {StatusCode} для /api/{Endpoint}: {Body}")]
    private static partial void LogHttpError(ILogger logger, int statusCode, string endpoint, string body);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error,
        Message = "Сетевая ошибка при запросе к /api/{Endpoint}")]
    private static partial void LogTransportError(ILogger logger, string endpoint, Exception exception);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error,
        Message = "Таймаут запроса к /api/{Endpoint} (лимит {Timeout})")]
    private static partial void LogTimeout(ILogger logger, string endpoint, TimeSpan timeout, Exception exception);
}
