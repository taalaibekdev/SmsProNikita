using System.Text;
using SmsProNikita.Models;

namespace SmsProNikita.Tests;

public sealed class DeliveryReportPushParserTests
{
    private const string SpecSample = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <report xmlns="http://Giper.mobi/schema/DeliveryReport">
            <dr>
                <id>A881726</id>
                <phone>
                    <number>996550123456</number>
                    <report>1</report>
                    <sendTime>20110909032815</sendTime>
                    <rcvTime>20110909032820</rcvTime>
                </phone>
                <phone>
                    <number>996550123457</number>
                    <report>1</report>
                    <sendTime>20110909032816</sendTime>
                    <rcvTime>20110909032821</rcvTime>
                </phone>
            </dr>
            <dr>
                <id>A887236</id>
                <phone>
                    <number>996550123458</number>
                    <report>1</report>
                    <sendTime>20110909032817</sendTime>
                    <rcvTime>20110909032822</rcvTime>
                </phone>
            </dr>
        </report>
        """;

    [Fact]
    public void SpecSampleIsParsed()
    {
        var push = DeliveryReportPushParser.Parse(SpecSample);

        Assert.Equal(2, push.Entries.Count);

        var first = push.Entries[0];
        Assert.Equal("A881726", first.Id);
        Assert.Equal(2, first.Phones.Count);

        var firstPhone = first.Phones[0];
        Assert.Equal("996550123456", firstPhone.Number);
        Assert.Equal(DeliveryReportCode.SentToOperator, firstPhone.Report);
        Assert.Equal(new DateTimeOffset(2011, 9, 9, 3, 28, 15, SmsProTime.BishkekOffset), firstPhone.SendTime);
        Assert.Equal(new DateTimeOffset(2011, 9, 9, 3, 28, 20, SmsProTime.BishkekOffset), firstPhone.ReceiveTime);

        var second = push.Entries[1];
        Assert.Equal("A887236", second.Id);
        var secondPhone = Assert.Single(second.Phones);
        Assert.Equal("996550123458", secondPhone.Number);
    }

    [Fact]
    public void StreamIsParsed()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SpecSample));

        var push = DeliveryReportPushParser.Parse(stream);

        Assert.Equal(2, push.Entries.Count);
    }

    [Fact]
    public void EmptyTimeFieldsBecomeNull()
    {
        const string Xml = """
            <report xmlns="http://Giper.mobi/schema/DeliveryReport">
                <dr>
                    <id>A1</id>
                    <phone>
                        <number>996550123456</number>
                        <report>0</report>
                        <sendTime></sendTime>
                        <rcvTime></rcvTime>
                    </phone>
                </dr>
            </report>
            """;

        var push = DeliveryReportPushParser.Parse(Xml);
        var phone = Assert.Single(Assert.Single(push.Entries).Phones);

        Assert.Equal(DeliveryReportCode.Queued, phone.Report);
        Assert.Null(phone.SendTime);
        Assert.Null(phone.ReceiveTime);
    }

    [Fact]
    public void InvalidPayloadIsRejected()
    {
        Assert.Throws<SmsProException>(() => DeliveryReportPushParser.Parse("<not-a-report/>"));
    }
}
