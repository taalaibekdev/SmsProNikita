using System.Text.RegularExpressions;

namespace SmsProNikita.Internal;

/// <summary>
/// Форматы полей протокола smspro.nikita.kg (PDF-спецификация, разделы 1 и 3).
/// Регулярные выражения генерируются на этапе компиляции source-генератором .NET 10.
/// </summary>
internal static partial class ProtocolRules
{
    /// <summary>Номер получателя: 996XXXXXXXXX или +996XXXXXXXXX.</summary>
    [GeneratedRegex(@"^\+?996\d{9}$", RegexOptions.CultureInvariant)]
    internal static partial Regex PhoneNumber();

    /// <summary>Имя отправителя: от 1 до 11 латинских букв, цифр, точек или тире.</summary>
    [GeneratedRegex(@"^[A-Za-z0-9.\-]{1,11}$", RegexOptions.CultureInvariant)]
    internal static partial Regex AlphanumericSender();

    /// <summary>Имя отправителя из 14 цифр.</summary>
    [GeneratedRegex(@"^\d{14}$", RegexOptions.CultureInvariant)]
    internal static partial Regex NumericSender();
}
