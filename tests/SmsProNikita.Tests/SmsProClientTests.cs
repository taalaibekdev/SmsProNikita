using System.Net;
using SmsProNikita.Models;
using SmsProNikita.Tests.TestDoubles;

namespace SmsProNikita.Tests;

public sealed class SmsProClientTests
{
    private const string SuccessResponse = """
        <?xml version="1.0" encoding="UTF-8"?>
        <response><id>A88726</id><status>0</status><phones>2</phones><smscnt>2</smscnt><message></message></response>
        """;

    [Fact]
    public async Task SendMessagePostsXmlToApiMessage()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);

        await client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("https://smspro.nikita.kg/api/message", handler.LastRequestUri?.AbsoluteUri);
        Assert.Contains("<login>login</login>", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("<pwd>passwd</pwd>", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("<id>A88726</id>", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("<phone>996550123456</phone>", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessageMapsSuccessResponse()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);

        var result = await client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("A88726", result.Id);
        Assert.Equal(SmsSendStatus.Success, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Phones);
        Assert.Equal(2, result.SmsCount);
        Assert.Equal(4, result.TotalBilledSms);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task SendMessageMapsErrorStatus()
    {
        const string Xml = """
            <response><id>A1</id><status>4</status><phones>1</phones><smscnt>1</smscnt><message>Недостаточно средств</message></response>
            """;
        var handler = new StubHttpMessageHandler(Xml);
        var client = TestClientFactory.Create(handler);

        var result = await client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(SmsSendStatus.InsufficientFunds, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Equal("Недостаточно средств", result.Message);
    }

    [Fact]
    public async Task SendMessageOmitsTestFlagByDefault()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);

        await client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.DoesNotContain("<test>", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessageSendsTestFlagFromRequest()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);
        var request = CreateRequest();
        request.Test = true;

        await client.SendMessageAsync(request, TestContext.Current.CancellationToken);

        Assert.Contains("<test>1</test>", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessageSendsTestFlagFromOptions()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var options = new SmsProOptions { Login = "login", Password = "passwd", DefaultTestMode = true };
        var client = TestClientFactory.Create(handler, options);

        await client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.Contains("<test>1</test>", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessageConvertsScheduledTimeToBishkek()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);
        var request = CreateRequest();
        request.ScheduledAt = new DateTimeOffset(2026, 1, 2, 20, 0, 0, TimeSpan.Zero);

        await client.SendMessageAsync(request, TestContext.Current.CancellationToken);

        Assert.Contains("<time>20260103020000</time>", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessageRejectsInvalidRequest()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);
        var request = CreateRequest();
        request.Id = "АВС123";

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.SendMessageAsync(request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendMessageRejectsNullRequest()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.SendMessageAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendMessageWrapsHttpError()
    {
        var handler = new StubHttpMessageHandler("<html>error</html>", HttpStatusCode.InternalServerError);
        var client = TestClientFactory.Create(handler);

        var exception = await Assert.ThrowsAsync<SmsProException>(
            () => client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken));

        Assert.Contains("HTTP 500", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessageWrapsInvalidXml()
    {
        var handler = new StubHttpMessageHandler("not xml at all");
        var client = TestClientFactory.Create(handler);

        await Assert.ThrowsAsync<SmsProException>(
            () => client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendMessageWrapsTransportError()
    {
        var handler = new StubHttpMessageHandler(new HttpRequestException("connection refused"));
        var client = TestClientFactory.Create(handler);

        var exception = await Assert.ThrowsAsync<SmsProException>(
            () => client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken));

        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task SendMessageWrapsTimeout()
    {
        var handler = new StubHttpMessageHandler(new TaskCanceledException("timeout"));
        var client = TestClientFactory.Create(handler);

        var exception = await Assert.ThrowsAsync<SmsProException>(
            () => client.SendMessageAsync(CreateRequest(), TestContext.Current.CancellationToken));

        Assert.Contains("Таймаут", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeliveryReportPostsToApiDr()
    {
        const string Xml = """
            <response>
                <status>0</status>
                <phone>
                    <number>996550123456</number>
                    <report>1</report>
                    <sendTime>20100921235958</sendTime>
                    <rcvTime></rcvTime>
                </phone>
                <phone>
                    <number>996550123457</number>
                    <report>3</report>
                    <sendTime>20100921235958</sendTime>
                    <rcvTime>20100921235959</rcvTime>
                </phone>
            </response>
            """;
        var handler = new StubHttpMessageHandler(Xml);
        var client = TestClientFactory.Create(handler);

        var report = await client.GetDeliveryReportAsync("A88726", "996550123456", TestContext.Current.CancellationToken);

        Assert.Equal("https://smspro.nikita.kg/api/dr", handler.LastRequestUri?.AbsoluteUri);
        Assert.Contains("<id>A88726</id>", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("<phone>996550123456</phone>", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.True(report.IsSuccess);
        Assert.Equal(2, report.Phones.Count);
        Assert.Equal(DeliveryReportCode.SentToOperator, report.Phones[0].Report);
        Assert.Null(report.Phones[0].ReceiveTime);
        Assert.Equal(DeliveryReportCode.Delivered, report.Phones[1].Report);
        Assert.Equal(
            new DateTimeOffset(2010, 9, 21, 23, 59, 59, SmsProTime.BishkekOffset),
            report.Phones[1].ReceiveTime);
    }

    [Fact]
    public async Task DeliveryReportOmitsEmptyPhoneFilter()
    {
        const string Xml = """<response><status>0</status></response>""";
        var handler = new StubHttpMessageHandler(Xml);
        var client = TestClientFactory.Create(handler);

        await client.GetDeliveryReportAsync("A88726", cancellationToken: TestContext.Current.CancellationToken);

        Assert.DoesNotContain("<phone>", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeliveryReportRejectsEmptyId()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.GetDeliveryReportAsync("  ", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeliveryReportMapsNotFoundStatus()
    {
        const string Xml = """<response><status>4</status></response>""";
        var handler = new StubHttpMessageHandler(Xml);
        var client = TestClientFactory.Create(handler);

        var report = await client.GetDeliveryReportAsync("A88726", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(SmsProResponseStatus.NotFound, report.Status);
        Assert.False(report.IsSuccess);
        Assert.Empty(report.Phones);
    }

    [Fact]
    public async Task PhoneInfoPostsToApiDefAndMapsResponse()
    {
        const string Xml = """
            <response xmlns="http://Giper.mobi/schema/PhoneDEF">
                <status>0</status>
                <region>Бишкек,Бишкек</region>
                <operator>MegaCom</operator>
                <timezone>6</timezone>
            </response>
            """;
        var handler = new StubHttpMessageHandler(Xml);
        var client = TestClientFactory.Create(handler);

        var info = await client.GetPhoneInfoAsync("996550123456", TestContext.Current.CancellationToken);

        Assert.Equal("https://smspro.nikita.kg/api/def", handler.LastRequestUri?.AbsoluteUri);
        Assert.Contains("xmlns=\"http://Giper.mobi/schema/PhoneDEF\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.True(info.IsSuccess);
        Assert.Equal("Бишкек,Бишкек", info.Region);
        Assert.Equal("MegaCom", info.Operator);
        Assert.Equal(6, info.Timezone);
    }

    [Fact]
    public async Task PhoneInfoRejectsEmptyPhone()
    {
        var handler = new StubHttpMessageHandler(SuccessResponse);
        var client = TestClientFactory.Create(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.GetPhoneInfoAsync(string.Empty, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task AccountInfoPostsToApiInfoAndMapsState(int state, bool expectedIsActive)
    {
        var xml = $"""
            <response xmlns="http://Giper.mobi/schema/Info">
                <status>0</status>
                <state>{state}</state>
                <account>20123.12</account>
                <smsprice>0.50</smsprice>
            </response>
            """;
        var handler = new StubHttpMessageHandler(xml);
        var client = TestClientFactory.Create(handler);

        var account = await client.GetAccountInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal("https://smspro.nikita.kg/api/info", handler.LastRequestUri?.AbsoluteUri);
        Assert.True(account.IsSuccess);
        Assert.Equal(expectedIsActive, account.IsActive);
        Assert.Equal(20123.12m, account.Account);
        Assert.Equal(0.50m, account.SmsPrice);
    }

    private static SendMessageRequest CreateRequest() => new()
    {
        Id = "A88726",
        Sender = "My-company",
        Text = "Any SMS message text",
        Phones = ["996550123456", "996550123457"],
    };
}
