using Microsoft.Data.Sqlite;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreThemes;

// Хранит "админ хочет, чтобы на ПК номер X стояла тема Y" — на СЕРВЕРЕ,
// в базе администратора. Нужно ровно для одного сценария: ПК был
// выключен, админ выбрал ему тему заранее — когда ПК включится и
// подключится, сервер сам пришлёт ему эту тему при первом же коннекте.
public static class НазначенныеТемы
{
    private static bool инициализировано;
    private static readonly object блокировка = new();

    private static SqliteConnection Open()
    {
        var con = SqliteDb.Open();

        lock (блокировка)
        {
            if (!инициализировано)
            {
                var cmd = con.CreateCommand();

                cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS PcThemeAssignments(
                    PcId INTEGER PRIMARY KEY,
                    ThemeId TEXT NOT NULL
                );
                """;

                cmd.ExecuteNonQuery();

                инициализировано = true;
            }
        }

        return con;
    }

    public static void Установить(int pcId, string themeId)
    {
        using var con = Open();
        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO PcThemeAssignments(PcId, ThemeId)
        VALUES($id,$t)
        ON CONFLICT(PcId) DO UPDATE SET ThemeId=$t;
        """;

        cmd.Parameters.AddWithValue("$id", pcId);
        cmd.Parameters.AddWithValue("$t", themeId);

        cmd.ExecuteNonQuery();
    }

    public static string? Получить(int pcId)
    {
        using var con = Open();
        var cmd = con.CreateCommand();

        cmd.CommandText = "SELECT ThemeId FROM PcThemeAssignments WHERE PcId=$id;";
        cmd.Parameters.AddWithValue("$id", pcId);

        return cmd.ExecuteScalar() as string;
    }
}