using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmsProNikita;
using SmsProNikita.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSmsProNikita(options =>
{
    options.Login = Environment.GetEnvironmentVariable("SMSPRO_LOGIN") ?? "login";
    options.Password = Environment.GetEnvironmentVariable("SMSPRO_PASSWORD") ?? "passwd";
    options.UseSsl = true;
    options.DefaultTestMode = true; // безопасный режим по умолчанию для примера
});

using var host = builder.Build();

var smsPro = host.Services.GetRequiredService<ISmsProClient>();

var sendResult = await smsPro.SendMessageAsync(new SendMessageRequest
{
    Id = MessageId.New(), // уникальный id: 12 латинских букв/цифр
    Sender = "SMSPRO.KG",
    Text = "Тестовое сообщение из SmsProNikita",
    Phones = ["996550123456"],
    // Отложенная отправка задаётся в бишкекском поясе (GMT+6):
    // ScheduledAt = SmsProTime.FromBishkek(DateTime.Today.AddDays(1).AddHours(10)),
});

Console.WriteLine($"Отправка: status={sendResult.Status}, phones={sendResult.Phones}, smsCnt={sendResult.SmsCount}");

if (sendResult.IsSuccess)
{
    var report = await smsPro.GetDeliveryReportAsync(sendResult.Id);
    foreach (var phone in report.Phones)
    {
        Console.WriteLine($"  {phone.Number}: {phone.Report} (отправлено: {phone.SendTime}, доставлено: {phone.ReceiveTime})");
    }
}

var account = await smsPro.GetAccountInfoAsync();
Console.WriteLine($"Баланс: {account.Account} (цена SMS: {account.SmsPrice}, активен: {account.IsActive})");

var subscriber = await smsPro.GetPhoneInfoAsync("996550123456");
Console.WriteLine($"Абонент: {subscriber.Region}, {subscriber.Operator}, GMT+{subscriber.Timezone}");
