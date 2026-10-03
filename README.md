# SmsProNikita

DI-клиент .NET 10 (C#) для XML-протокола SMS-шлюза **smspro.nikita.kg**:

- отправка SMS (`POST /api/message`);
- получение отчётов о доставке — опросом шлюза (`POST /api/dr`) и разбором push-отчётов,
  которые шлюз присылает на ваш URL;
- информация об абоненте по номеру телефона: регион, оператор, часовой пояс (`POST /api/def`);
- баланс счёта, цена SMS, активность аккаунта (`POST /api/info`).

Библиотека не использует рефлексию и `XmlSerializer`: XML формируется и разбирается явно
(устойчиво к namespace'ам, защищено от XXE, пригодно для trimming/AOT-сценариев).

## Требования

- .NET 10 (net10.0). Для старых платформ используйте версию 0.x пакета.

## Установка

```bash
dotnet add package SmsProNikita
```

## Регистрация в DI

```csharp
using SmsProNikita;

var builder = WebApplication.CreateBuilder(args);

// Вариант 1 — настройка кодом
builder.Services.AddSmsProNikita(options =>
{
    options.Login = "login";
    options.Password = "passwd";
    options.UseSsl = true; // https://smspro.nikita.kg
});

// Вариант 2 — настройка из appsettings.json (секция "SmsPro" по умолчанию)
// builder.Services.AddSmsProNikita(builder.Configuration);
```

`appsettings.json`:

```json
{
  "SmsPro": {
    "Login": "login",
    "Password": "passwd",
    "UseSsl": true,
    "DefaultTestMode": false,
    "Timeout": "00:00:30"
  }
}
```

Параметры проверяются при старте приложения (`ValidateOnStart`): пустой `Login`/`Password`,
`Host` со схемой (`https://...`) или нулевой `Timeout` останавливают запуск с понятной ошибкой,
а не приводят к падению первого запроса.

## Отправка SMS

```csharp
public sealed class NotificationService(ISmsProClient smsPro)
{
    public async Task NotifyAsync(CancellationToken ct)
    {
        var result = await smsPro.SendMessageAsync(new SendMessageRequest
        {
            Id = MessageId.New(),          // уникальный id: 12 латинских букв/цифр
            Sender = "My-company",         // до 11 латинских букв/цифр/точек/тире или 14 цифр
            Text = "Ваш код подтверждения: 4571",
            Phones = ["996550123456", "996550123457"],   // до 50 номеров в пакете
            // ScheduledAt = SmsProTime.FromBishkek(DateTime.Today.AddHours(10)), // отложенная отправка
            // Test = true,                 // тестовый прогон без отправки и списания
        }, ct);

        if (!result.IsSuccess)
        {
            // result.Status — SmsSendStatus (InsufficientFunds, InvalidPhoneNumber, ...)
            // result.Message — необязательное текстовое описание ошибки от сервера
            throw new InvalidOperationException($"Отправка не удалась: {result.Status} {result.Message}");
        }

        // result.TotalBilledSms == result.Phones * result.SmsCount
    }
}
```

Если шлюз ответил `SmsSendStatus.ProcessingTimeout` (код 9), запрос повторяют с **тем же** `Id`
через 5-10 секунд: шлюз отсекает дубли по id, поэтому повтор не приведёт к двойной отправке.

## Уникальность Id

`Id` — ключ идемпотентности: шлюз использует его, чтобы отбросить дубли, и по нему же возвращает
отчёты о доставке. Уникальность обеспечивает отправитель.

- **одно новое сообщение — один новый `Id`** (один `Id` покрывает весь пакет до 50 номеров);
- **повтор после таймаута/сетевой ошибки — тот же `Id`**;
- **тот же `Id` с другим текстом — ошибка**: шлюз вернёт `SmsSendStatus.DuplicateId` (код 10);
- формат: до 12 символов, только `A-Z`, `a-z`, `0-9` (`-`, `_`, кириллица недопустимы).

Готовые генераторы (пространство id — 12 знаков base36, 36¹² ≈ 4,7·10¹⁸ комбинаций):

| Метод | Вид | Когда использовать |
| --- | --- | --- |
| `MessageId.New()` | 6 знаков — минуты с 1970 в base36, 6 — случайные | по умолчанию: id сортируются по времени, совпадение вероятно лишь при ~66 000 id в минуту |
| `MessageId.NewRandom()` | 12 случайных знаков | генерация id пачками; хронологию храните сами |

```csharp
var request = new SendMessageRequest
{
    Id = MessageId.New(),          // например: 0hrt28bqtd0q
    Sender = "My-company",
    Text = "Ваш код подтверждения: 4571",
    Phones = ["996550123456"],
};
```

Оба метода используют `RandomNumberGenerator` (криптослучайный источник), а не `Random`:
у `Random`, запущенного в разных процессах в одну и ту же миллисекунду, могут совпасть
начальные значения — и тогда совпадут целые последовательности id.

Если сообщение сохраняется в собственную БД (outbox-паттерн), сделайте `Id` частью схемы:
уникальный индекс по `Id` + сохранение статуса до отправки. Тогда «свой» ключ записи и `Id`
протокола — одно и то же значение, а повторный запуск обработчика переиспользует уже
созданный `Id` вместо генерации нового:

```csharp
// 1) сначала фиксируем намерение в БД (INSERT ... ON CONFLICT DO NOTHING по уникальному Id)
var entity = await db.Outbox.FirstOrDefaultAsync(m => m.OperationId == operationId, ct);
if (entity is null)
{
    entity = new OutboxMessage { Id = MessageId.New(), OperationId = operationId, Status = "new" };
    db.Outbox.Add(entity);
    await db.SaveChangesAsync(ct); // уникальный индекс по OperationId защищает от гонок
}

// 2) отправляем, переживая ретраи: Id уже сохранён, поэтому повтор безопасен
if (entity.Status == "new")
{
    var result = await smsPro.SendMessageAsync(new SendMessageRequest
    {
        Id = entity.Id,
        Sender = "My-company",
        Text = entity.Text,
        Phones = entity.Phones,
    }, ct);

    entity.Status = result.IsSuccess ? "sent" : "failed";
    await db.SaveChangesAsync(ct);
}
```

Чего делать не стоит:

- `Id = Guid.NewGuid().ToString("N")[..12]` — это только 12 шестнадцатеричных знаков, то есть
  48 случайных бит вместо 62: коллизии становятся ожидаемыми примерно на 17 млн сообщений;
- `Id = DateTime.Now.ToString("yyMMddHHmmss")` — 10 знаков времени и ни одного случайного:
  два сообщения в одну секунду (или два инстанса) получат одинаковый `Id`;
- `Id = "ABC1234"` константой в коде — второй запуск примера/теста получит «дубль»;
- переводить часы на сервере назад, если уникальность держится только на метке времени:
  префикс повторится — уникальный индекс в БД снимает этот риск.

## Часовые пояса

Протокол передаёт время в **бишкекском** поясе (GMT+6) в формате `YYYYMMDDhhmmss`. Клиент
приводит значения сам, поэтому часовой пояс сервера приложения не влияет на результат:

```csharp
// Отложенная отправка на 10:00 по Бишкеку
request.ScheduledAt = SmsProTime.FromBishkek(new DateTime(2026, 1, 3, 10, 0, 0));

// Момент времени из любого пояса
request.ScheduledAt = SmsProTime.ToBishkek(DateTimeOffset.UtcNow.AddHours(1));

SmsProTime.BishkekOffset;                  // +06:00
SmsProTime.GetNowBishkek(timeProvider);    // текущее время по Бишкеку
```

В отчётах о доставке `SendTime`/`ReceiveTime` — это `DateTimeOffset` с тем же смещением
`+06:00` (или `null`, если сообщение ещё не отправлено / отчёт не получен).

## Получение отчёта о доставке (опрос шлюза)

```csharp
var report = await smsPro.GetDeliveryReportAsync(id: result.Id, ct: ct);

foreach (var phone in report.Phones)
{
    Console.WriteLine($"{phone.Number}: {phone.Report} (доставлено: {phone.ReceiveTime})");
}
```

## Приём push-отчётов на своём сервере

Если в личном кабинете включена доставка отчётов на ваш URL, разберите входящее тело запроса так:

```csharp
app.MapPost("/sms/delivery-report", async (HttpRequest request) =>
{
    var report = DeliveryReportPushParser.Parse(request.Body);

    foreach (var entry in report.Entries)
    foreach (var phone in entry.Phones)
    {
        // сопоставляйте entry.Id (id вашей исходной отправки) + phone.Number
    }

    return Results.Ok(); // шлюз ожидает HTTP 200 OK, иначе повторит доставку 3 раза с интервалом 10 минут
});
```

Один отчёт может содержать несколько транзакций (`dr`), в каждой — несколько номеров (`phone`).
Разбор запрещает DTD и внешние сущности, поэтому публичный эндпоинт защищён от XXE.

## Информация об абоненте

```csharp
var info = await smsPro.GetPhoneInfoAsync("996550123456", ct);
if (info.IsSuccess)
{
    Console.WriteLine($"{info.Region}, {info.Operator}, GMT+{info.Timezone}");
}
```

Информация доступна только по номерам GSM-сетей Кыргызстана; запрос не тарифицируется.

## Баланс счёта

```csharp
var account = await smsPro.GetAccountInfoAsync(ct);
Console.WriteLine($"Баланс: {account.Account}, цена SMS: {account.SmsPrice}, активен: {account.IsActive}");
```

## Обработка ошибок

- **Транспортные/протокольные ошибки** (сеть недоступна, таймаут, HTTP-код не 2xx,
  неразбираемый XML) выбрасываются как `SmsProException`.
- **Бизнес-статусы протокола** (недостаточно средств, неверная авторизация, невалидный номер
  и т.п.) НЕ выбрасываются как исключения — они возвращаются в `Status` соответствующего
  результата (`SendMessageResult.Status`, `DeliveryReportResult.Status` и т.д.), чтобы вызывающий
  код мог сам решить, что делать (повторить, залогировать, показать пользователю).
- Обмен с шлюзом логируется через `ILogger<SmsProClient>`: диагностические сообщения — `Debug`,
  принятые шлюзом сообщения — `Information`, отказы и ошибки — `Warning`/`Error`.

## Соответствие протоколу

| Метод API | Возможность | Коды статусов |
| --- | --- | --- |
| `/api/message` | отправка, `test=1`, отложенная отправка | `SmsSendStatus` 0-11 |
| `/api/dr` | отчёт по id и (опционально) номеру | `SmsProResponseStatus` 0-5 |
| `/api/def` | регион, оператор, часовой пояс абонента | `SmsProResponseStatus` 0-4 |
| `/api/info` | баланс, цена SMS, активность аккаунта | `SmsProResponseStatus` 0-3 |
| push на URL партнёра | отчёты по нескольким транзакциям | `DeliveryReportCode` 0-7 |

Значения кодов отличаются между методами — смысл для конкретного метода указан в XML-документации
соответствующего перечисления.

## Разработка

```bash
dotnet restore SmsProNikita.slnx
dotnet build   SmsProNikita.slnx -c Release
dotnet test    SmsProNikita.slnx -c Release     # xUnit.net v3 + Microsoft Testing Platform
dotnet pack    src/SmsProNikita/SmsProNikita.csproj -c Release -o artifacts
```

Проект тестов — самостоятельный исполняемый файл (xUnit.net v3), поэтому тесты можно запустить
и без `dotnet test`:

```bash
dotnet run --project tests/SmsProNikita.Tests -c Release
```

Структура решения:

```
src/SmsProNikita          библиотека (net10.0)
samples/SampleConsole     консольный пример
tests/SmsProNikita.Tests  тесты xUnit.net v3
```

Качество кода: `AnalysisLevel=latest-recommended`, стиль из `.editorconfig` применяется при сборке
(`EnforceCodeStyleInBuild`), предупреждения считаются ошибками (`TreatWarningsAsErrors`),
SourceLink и символьный пакет включены.

## Лицензия

MIT
