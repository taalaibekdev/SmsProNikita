using SmsProNikita.Internal;
using SmsProNikita.Models;

namespace SmsProNikita;

/// <summary>
/// Разбирает XML, который шлюз smspro.nikita.kg присылает POST-запросом на URL партнёра,
/// настроенный в личном кабинете (раздел 2.1 протокола, "Доставка отчётов от SMS-шлюза
/// на сервер партнера").
/// </summary>
/// <remarks>
/// После обработки такого запроса ваш эндпоинт должен вернуть HTTP 200 OK, иначе шлюз
/// повторит доставку 3 раза с интервалом 10 минут. Разбор устойчив к namespace'ам
/// (шлюз присылает <c>xmlns="http://Giper.mobi/schema/DeliveryReport"</c>) и запрещает
/// DTD/внешние сущности, поэтому эндпоинт защищён от XXE.
/// </remarks>
public static class DeliveryReportPushParser
{
    /// <summary>Разобрать тело входящего запроса (XML-строка) в модель <see cref="DeliveryReportPush"/>.</summary>
    /// <param name="xml">Тело запроса.</param>
    /// <exception cref="SmsProException">Если XML некорректен или его корневой элемент не <c>report</c>.</exception>
    public static DeliveryReportPush Parse(string xml) => ProtocolXml.ParsePush(xml);

    /// <summary>Разобрать тело входящего запроса (поток, например Request.Body) в модель <see cref="DeliveryReportPush"/>.</summary>
    /// <param name="xmlStream">Поток с XML-телом запроса.</param>
    /// <exception cref="SmsProException">Если XML некорректен или его корневой элемент не <c>report</c>.</exception>
    public static DeliveryReportPush Parse(Stream xmlStream) => ProtocolXml.ParsePush(xmlStream);
}
