using System;
using System.Security.Cryptography;

namespace серьёзный.Core.CoreSecurity;

// PBKDF2-SHA256 со случайной солью на каждый пароль. Формат строки:
// "PBKDF2$<итерации>$<соль-base64>$<хеш-base64>". Префикс "PBKDF2$"
// служит маркером формата — по нему Авторизовать() отличает уже
// хешированные пароли от старых записей, хранившихся открытым
// текстом до перехода на хеширование.
public static class PasswordHasher
{
    private const string Префикс = "PBKDF2";

    private const int Итерации = 100_000;

    private const int РазмерСоли = 16;

    private const int РазмерХеша = 32;

    public static string Hash(string пароль)
    {
        var соль = RandomNumberGenerator.GetBytes(РазмерСоли);

        var хеш = Rfc2898DeriveBytes.Pbkdf2(
            пароль,
            соль,
            Итерации,
            HashAlgorithmName.SHA256,
            РазмерХеша);

        return
            Префикс + "$" +
            Итерации + "$" +
            Convert.ToBase64String(соль) + "$" +
            Convert.ToBase64String(хеш);
    }

    public static bool IsHashed(string? значение)
    {
        return !string.IsNullOrEmpty(значение) &&
               значение.StartsWith(Префикс + "$", StringComparison.Ordinal);
    }

    public static bool Verify(string пароль, string хешированноеЗначение)
    {
        try
        {
            var части = хешированноеЗначение.Split('$');

            if (части.Length != 4 || части[0] != Префикс)
                return false;

            var итерации = int.Parse(части[1]);
            var соль = Convert.FromBase64String(части[2]);
            var ожидаемыйХеш = Convert.FromBase64String(части[3]);

            var вычисленныйХеш = Rfc2898DeriveBytes.Pbkdf2(
                пароль,
                соль,
                итерации,
                HashAlgorithmName.SHA256,
                ожидаемыйХеш.Length);

            return CryptographicOperations.FixedTimeEquals(
                вычисленныйХеш,
                ожидаемыйХеш);
        }
        catch
        {
            return false;
        }
    }
}