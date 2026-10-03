using SmsProNikita.Internal;
using SmsProNikita.Models;

namespace SmsProNikita.Tests;

public sealed class ProtocolXmlTests
{
    [Fact]
    public void MessageRequestMatchesProtocolExample()
    {
        var xml = ProtocolXml.BuildMessageRequest(
            login: "login",
            pwd: "passwd",
            id: "A88726",
            sender: "My-company",
            text: "Any SMS message text",
            time: "20100921235957",
            phones: ["996550123456", "996550123457"],
            test: true);

        Assert.Equal(
            """<?xml version="1.0" encoding="utf-8"?><message><login>login</login><pwd>passwd</pwd><id>A88726</id><sender>My-company</sender><text>Any SMS message text</text><time>20100921235957</time><phones><phone>996550123456</phone><phone>996550123457</phone></phones><test>1</test></message>""",
            xml);
    }

    [Fact]
    public void MessageRequestOmitsOptionalElements()
    {
        var xml = ProtocolXml.BuildMessageRequest(
            login: "login",
            pwd: "passwd",
            id: "A88726",
            sender: "My-company",
            text: "Any SMS message text",
            time: null,
            phones: ["996550123456"],
            test: false);

        Assert.DoesNotContain("<time>", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("<test>", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void DrRequestMatchesProtocolExample()
    {
        var xml = ProtocolXml.BuildDrRequest("login", "passwd", "A88726", "996550123456");

        Assert.Equal(
            """<?xml version="1.0" encoding="utf-8"?><dr><login>login</login><pwd>passwd</pwd><id>A88726</id><phone>996550123456</phone></dr>""",
            xml);
    }

    [Fact]
    public void DrRequestOmitsPhoneWhenNotSpecified()
    {
        var xml = ProtocolXml.BuildDrRequest("login", "passwd", "A88726", phone: null);

        Assert.DoesNotContain("<phone>", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void DefRequestCarriesPhoneDefNamespace()
    {
        var xml = ProtocolXml.BuildDefRequest("login", "passwd", "996550123456");

        Assert.Equal(
            """<?xml version="1.0" encoding="utf-8"?><def xmlns="http://Giper.mobi/schema/PhoneDEF"><login>login</login><pwd>passwd</pwd><phone>996550123456</phone></def>""",
            xml);
    }

    [Fact]
    public void InfoRequestCarriesInfoNamespace()
    {
        var xml = ProtocolXml.BuildInfoRequest("login", "passwd");

        Assert.Equal(
            """<?xml version="1.0" encoding="utf-8"?><info xmlns="http://Giper.mobi/schema/Info"><login>login</login><pwd>passwd</pwd></info>""",
            xml);
    }

    [Fact]
    public void MessageResponseIsParsed()
    {
        const string Xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <response>
                <id>A88726</id>
                <status>0</status>
                <phones>2</phones>
                <smscnt>2</smscnt>
                <message></message>
            </response>
            """;

        var result = ProtocolXml.ParseMessageResponse(Xml);

        Assert.Equal("A88726", result.Id);
        Assert.Equal(SmsSendStatus.Success, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Phones);
        Assert.Equal(2, result.SmsCount);
        Assert.Equal(4, result.TotalBilledSms);
        Assert.Null(result.Message);
    }

    [Fact]
    public void RootNamespaceIsIgnored()
    {
        const string Xml = """<response xmlns="http://Giper.mobi/schema/Message"><status>4</status></response>""";

        var result = ProtocolXml.ParseMessageResponse(Xml);

        Assert.Equal(SmsSendStatus.InsufficientFunds, result.Status);
    }

    [Fact]
    public void DefResponseIsParsed()
    {
        const string Xml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <response xmlns="http://Giper.mobi/schema/PhoneDEF">
                <status>0</status>
                <region>Бишкек,Бишкек</region>
                <operator>MegaCom</operator>
                <timezone>6</timezone>
            </response>
            """;

        var result = ProtocolXml.ParseDefResponse(Xml);

        Assert.True(result.IsSuccess);
        Assert.Equal("Бишкек,Бишкек", result.Region);
        Assert.Equal("MegaCom", result.Operator);
        Assert.Equal(6, result.Timezone);
    }

    [Fact]
    public void DefResponseWithoutPayloadKeepsUnknownTimezone()
    {
        const string Xml = """<response><status>4</status></response>""";

        var result = ProtocolXml.ParseDefResponse(Xml);

        Assert.Equal(SmsProResponseStatus.NotFound, result.Status);
        Assert.Null(result.Region);
        Assert.Null(result.Operator);
        Assert.Equal(-1, result.Timezone);
    }

    [Fact]
    public void InfoResponseIsParsed()
    {
        const string Xml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <response xmlns="http://Giper.mobi/schema/Info">
                <status>0</status>
                <state>0</state>
                <account>20123.12</account>
                <smsprice>0.50</smsprice>
            </response>
            """;

        var result = ProtocolXml.ParseInfoResponse(Xml);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsActive);
        Assert.Equal(20123.12m, result.Account);
        Assert.Equal(0.50m, result.SmsPrice);
    }

    [Fact]
    public void DrResponseIsParsed()
    {
        const string Xml = """
            <?xml version="1.0" encoding="UTF-8"?>
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

        var report = ProtocolXml.ParseDrResponse(Xml);

        Assert.Equal(SmsProResponseStatus.Ok, report.Status);
        Assert.Equal(2, report.Phones.Count);
        Assert.Equal(DeliveryReportCode.SentToOperator, report.Phones[0].Report);
        Assert.Equal(new DateTimeOffset(2010, 9, 21, 23, 59, 58, SmsProTime.BishkekOffset), report.Phones[0].SendTime);
        Assert.Null(report.Phones[0].ReceiveTime);
        Assert.Equal(DeliveryReportCode.Delivered, report.Phones[1].Report);
        Assert.Equal(new DateTimeOffset(2010, 9, 21, 23, 59, 59, SmsProTime.BishkekOffset), report.Phones[1].ReceiveTime);
    }

    [Fact]
    public void DocumentTypeDefinitionIsRejected()
    {
        // Классическая XXE-попытка: внешняя сущность подставляется в id отчёта.
        const string Xml = """
            <?xml version="1.0"?>
            <!DOCTYPE report [ <!ENTITY xxe SYSTEM "file:///c:/windows/win.ini"> ]>
            <report><dr><id>&xxe;</id></dr></report>
            """;

        Assert.Throws<SmsProException>(() => ProtocolXml.ParsePush(Xml));
    }

    [Fact]
    public void EmptyStringIsRejected()
    {
        Assert.Throws<ArgumentException>(() => ProtocolXml.ParseMessageResponse(string.Empty));
    }

    [Fact]
    public void InvalidXmlIsWrappedInSmsProException()
    {
        var exception = Assert.Throws<SmsProException>(() => ProtocolXml.ParseMessageResponse("not xml at all"));

        Assert.Contains("некорректный XML", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyStreamIsWrappedInSmsProException()
    {
        using var stream = new MemoryStream();

        Assert.Throws<SmsProException>(() => ProtocolXml.ParsePush(stream));
    }

    [Fact]
    public void UnexpectedRootElementIsRejected()
    {
        const string Xml = """<unexpected><status>0</status></unexpected>""";

        var exception = Assert.Throws<SmsProException>(() => ProtocolXml.ParseMessageResponse(Xml));

        Assert.Contains("<unexpected>", exception.Message, StringComparison.Ordinal);
    }
}
