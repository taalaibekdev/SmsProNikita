using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using SmsProNikita.Models;

namespace SmsProNikita.Internal;

/// <summary>
/// Явная работа с XML-протоколом smspro.nikita.kg: формирование запросов и разбор ответов.
/// </summary>
/// <remarks>
/// Раньше здесь использовался <see cref="System.Xml.Serialization.XmlSerializer"/>, но он
/// требует, чтобы DTO были публичными (то есть протекали в публичный API пакета), работает
/// через рефлексию и несовместим с trimming/AOT. Явное построение XML через
/// <see cref="XElement"/> и разбор по локальным именам элементов решают все три проблемы.
/// Namespace'ы при разборе игнорируются: шлюз не всегда следует документации
/// (например, /api/message иногда отвечает &lt;response xmlns="..."&gt;).
/// </remarks>
internal static class ProtocolXml
{
    private const string DefNamespace = "http://Giper.mobi/schema/PhoneDEF";
    private const string InfoNamespace = "http://Giper.mobi/schema/Info";
    private const string XmlDeclaration = """<?xml version="1.0" encoding="utf-8"?>""";

    /// <summary>Верхняя граница размера разбираемого XML — защита от «XML-бомб» на публичном push-эндпоинте.</summary>
    private const int MaxCharactersInDocument = 8 * 1024 * 1024;

    // ---------- формирование запросов ----------

    public static string BuildMessageRequest(
        string login,
        string pwd,
        string id,
        string sender,
        string text,
        string? time,
        IReadOnlyCollection<string> phones,
        bool test)
    {
        var root = new XElement(
            "message",
            new XElement("login", login),
            new XElement("pwd", pwd),
            new XElement("id", id),
            new XElement("sender", sender),
            new XElement("text", text),
            time is null ? null : new XElement("time", time),
            new XElement("phones", phones.Select(phone => new XElement("phone", phone))),
            test ? new XElement("test", 1) : null);

        return ToXml(root);
    }

    public static string BuildDrRequest(string login, string pwd, string id, string? phone)
    {
        var root = new XElement(
            "dr",
            new XElement("login", login),
            new XElement("pwd", pwd),
            new XElement("id", id),
            phone is null ? null : new XElement("phone", phone));

        return ToXml(root);
    }

    public static string BuildDefRequest(string login, string pwd, string phone)
    {
        XNamespace ns = DefNamespace;

        var root = new XElement(
            ns + "def",
            new XElement(ns + "login", login),
            new XElement(ns + "pwd", pwd),
            new XElement(ns + "phone", phone));

        return ToXml(root);
    }

    public static string BuildInfoRequest(string login, string pwd)
    {
        XNamespace ns = InfoNamespace;

        var root = new XElement(
            ns + "info",
            new XElement(ns + "login", login),
            new XElement(ns + "pwd", pwd));

        return ToXml(root);
    }

    private static string ToXml(XElement root)
    {
        // Объявление формируется вручную: XDocument.Save() подставил бы кодировку TextWriter
        // (UTF-16), а тело запроса уходит как UTF-8 (StringContent).
        return XmlDeclaration + root.ToString(SaveOptions.DisableFormatting);
    }

    // ---------- разбор ответов ----------

    public static SendMessageResult ParseMessageResponse(string xml)
    {
        var root = LoadRoot(xml, "response");

        return new SendMessageResult
        {
            Id = Value(root, "id") ?? string.Empty,
            Status = (SmsSendStatus)Int(root, "status"),
            Phones = Int(root, "phones"),
            SmsCount = Int(root, "smscnt"),
            Message = NullIfEmpty(Value(root, "message")),
        };
    }

    public static DeliveryReportResult ParseDrResponse(string xml)
    {
        var root = LoadRoot(xml, "response");

        return new DeliveryReportResult
        {
            Status = (SmsProResponseStatus)Int(root, "status"),
            Phones = Children(root, "phone").Select(ParsePhoneReport).ToList(),
        };
    }

    public static PhoneInfoResult ParseDefResponse(string xml)
    {
        var root = LoadRoot(xml, "response");

        return new PhoneInfoResult
        {
            Status = (SmsProResponseStatus)Int(root, "status"),
            Region = NullIfEmpty(Value(root, "region")),
            Operator = NullIfEmpty(Value(root, "operator")),
            // Поля region/operator/timezone присутствуют только при status=0; -1 означает "неизвестно".
            Timezone = Int(root, "timezone", -1),
        };
    }

    public static AccountInfoResult ParseInfoResponse(string xml)
    {
        var root = LoadRoot(xml, "response");

        return new AccountInfoResult
        {
            Status = (SmsProResponseStatus)Int(root, "status"),
            IsActive = Int(root, "state") == 0,
            Account = Decimal(root, "account"),
            SmsPrice = Decimal(root, "smsprice"),
        };
    }

    public static DeliveryReportPush ParsePush(string xml) => MapPush(LoadRoot(xml, "report"));

    public static DeliveryReportPush ParsePush(Stream xmlStream) => MapPush(LoadRoot(xmlStream, "report"));

    private static DeliveryReportPush MapPush(XElement root) => new()
    {
        Entries = Children(root, "dr")
            .Select(dr => new DeliveryReportPushEntry
            {
                Id = Value(dr, "id") ?? string.Empty,
                Phones = Children(dr, "phone").Select(ParsePhoneReport).ToList(),
            })
            .ToList(),
    };

    private static PhoneDeliveryReport ParsePhoneReport(XElement element) => new()
    {
        Number = Value(element, "number") ?? string.Empty,
        Report = (DeliveryReportCode)Int(element, "report"),
        SendTime = SmsProDateTime.FromProtocolString(Value(element, "sendTime")),
        ReceiveTime = SmsProDateTime.FromProtocolString(Value(element, "rcvTime")),
    };

    // ---------- низкоуровневые помощники ----------

    private static XElement LoadRoot(string xml, string expectedRootName)
    {
        ArgumentException.ThrowIfNullOrEmpty(xml);

        try
        {
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, CreateReaderSettings());
            return ValidateRoot(XDocument.Load(reader, LoadOptions.None), expectedRootName);
        }
        catch (XmlException ex)
        {
            throw new SmsProException($"Не удалось разобрать XML как <{expectedRootName}>: некорректный XML.", ex);
        }
    }

    private static XElement LoadRoot(Stream xmlStream, string expectedRootName)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);

        try
        {
            using var reader = XmlReader.Create(xmlStream, CreateReaderSettings());
            return ValidateRoot(XDocument.Load(reader, LoadOptions.None), expectedRootName);
        }
        catch (XmlException ex)
        {
            throw new SmsProException($"Не удалось разобрать XML как <{expectedRootName}>: некорректный XML.", ex);
        }
    }

    private static XElement ValidateRoot(XDocument document, string expectedRootName)
    {
        var root = document.Root
            ?? throw new SmsProException($"XML пуст: ожидался корневой элемент <{expectedRootName}>.");

        if (!string.Equals(root.Name.LocalName, expectedRootName, StringComparison.Ordinal))
        {
            throw new SmsProException(
                $"Неожиданный корневой элемент <{root.Name.LocalName}>: ожидался <{expectedRootName}>.");
        }

        return root;
    }

    /// <summary>
    /// Настройки чтения: DTD запрещены, внешние сущности не разрешаются (защита от XXE),
    /// размер документа ограничен. Экземпляр создаётся на каждый разбор, так как
    /// <see cref="XmlReaderSettings"/> не гарантирует потокобезопасность.
    /// </summary>
    private static XmlReaderSettings CreateReaderSettings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = MaxCharactersInDocument,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        CloseInput = false,
    };

    private static XElement? Child(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, localName, StringComparison.Ordinal));

    private static IEnumerable<XElement> Children(XElement parent, string localName) =>
        parent.Elements().Where(e => string.Equals(e.Name.LocalName, localName, StringComparison.Ordinal));

    private static string? Value(XElement parent, string localName) => Child(parent, localName)?.Value;

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private static int Int(XElement parent, string localName, int defaultValue = 0) =>
        int.TryParse(Value(parent, localName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;

    private static decimal Decimal(XElement parent, string localName) =>
        decimal.TryParse(Value(parent, localName), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
}
