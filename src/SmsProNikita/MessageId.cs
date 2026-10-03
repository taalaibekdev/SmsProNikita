using System.Security.Cryptography;

namespace SmsProNikita;

/// <summary>
/// Генератор значений для <see cref="Models.SendMessageRequest.Id"/>.
/// </summary>
/// <remarks>
/// Протокол требует, чтобы id был уникальным в пределах аккаунта: по нему шлюз
/// отсекает дубли и сопоставляет отчёты о доставке. Поэтому id нельзя
/// фиксировать в коде — генерируйте новое значение для каждого нового сообщения.
/// </remarks>
public static class MessageId
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    /// <summary>Длина id, допустимая протоколом.</summary>
    public const int Length = 12;

    /// <summary>Сколько знаков отведено под метку времени (остальное — случайная часть).</summary>
    private const int TimePartLength = 6;

    /// <summary>
    /// Создать id, упорядоченный по времени: 6 знаков — минуты с 1970 года в base36,
    /// 6 знаков — криптослучайные.
    /// </summary>
    /// <remarks>
    /// Лексикографический порядок id совпадает с хронологическим, поэтому id удобно
    /// хранить и отлаживать. Запас времени — 36^6 минут, то есть примерно до 6100 года.
    /// В пределах одной минуты случайная часть даёт 36^6 ≈ 2,18 млрд комбинаций:
    /// заметная вероятность совпадения появляется лишь при ~66 000 id в одну минуту
    /// (более тысячи отправок в секунду) — для SMS-шлюза это недостижимо.
    /// Если id нужны миллионами в короткий промежуток, используйте <see cref="NewRandom"/>.
    /// Абсолютную гарантию уникальности даёт уникальный индекс в вашей БД (см. README).
    /// </remarks>
    public static string New()
    {
        var minutes = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        var chars = new char[Length];

        for (var i = TimePartLength - 1; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(minutes % Alphabet.Length)];
            minutes /= Alphabet.Length;
        }

        RandomNumberGenerator.GetItems<char>(Alphabet, chars.AsSpan(TimePartLength));
        return new string(chars);
    }

    /// <summary>
    /// Создать id только из случайных знаков: 12 знаков, 36^12 ≈ 4,7·10^18 комбинаций.
    /// </summary>
    /// <remarks>
    /// Хронологию такой id не хранит — время отправки придётся брать из своей БД.
    /// Зато вероятность совпадения остаётся пренебрежимо малой даже при генерации
    /// сотен миллионов значений (например, ~10^-7 при миллионе id).
    /// </remarks>
    public static string NewRandom() => RandomNumberGenerator.GetString(Alphabet, Length);
}
