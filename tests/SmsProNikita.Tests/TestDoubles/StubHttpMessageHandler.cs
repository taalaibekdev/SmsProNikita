using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace SmsProNikita.Tests.TestDoubles;

/// <summary>
/// Подставной HTTP-обработчик: запоминает отправленный запрос и возвращает заранее
/// заданный ответ (или выбрасывает заданное исключение).
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage? _response;
    private readonly Exception? _exception;

    public StubHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/xml"),
        };
    }

    public StubHttpMessageHandler(Exception exception) => _exception = exception;

    public Uri? LastRequestUri { get; private set; }

    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return _exception is not null ? throw _exception : _response!;
    }
}

/// <summary>Сборка <see cref="SmsProClient"/> поверх подставного обработчика.</summary>
internal static class TestClientFactory
{
    public static SmsProOptions DefaultOptions { get; } = new()
    {
        Login = "login",
        Password = "passwd",
        Host = "smspro.nikita.kg",
        UseSsl = true,
    };

    public static SmsProClient Create(
        StubHttpMessageHandler handler,
        SmsProOptions? options = null,
        ILogger<SmsProClient>? logger = null)
    {
        var effectiveOptions = options ?? DefaultOptions;
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = effectiveOptions.BaseAddress,
            Timeout = effectiveOptions.Timeout,
        };

        return new SmsProClient(
            httpClient,
            Options.Create(effectiveOptions),
            logger ?? NullLogger<SmsProClient>.Instance);
    }
}
