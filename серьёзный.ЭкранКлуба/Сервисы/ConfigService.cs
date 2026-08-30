using серьёзный.ЭкранКлуба.Модели;

namespace серьёзный.ЭкранКлуба.Сервисы;

public static class ConfigService
{
    public static Config Загрузить()
    {
        using var db =
            СервисБазыЭкрана001.Открыть();

        using var cmd = db.CreateCommand();

        cmd.CommandText =
            "SELECT AdminName, Password, Title FROM ScreenConfig WHERE Id=1;";

        using var r = cmd.ExecuteReader();

        r.Read();

        return new Config
        {
            AdminName = r.GetString(0),
            Password = r.GetString(1),
            Title = r.GetString(2)
        };
    }

    public static void Сохранить(
        Config config)
    {
        using var db =
            СервисБазыЭкрана001.Открыть();

        using var cmd = db.CreateCommand();

        cmd.CommandText = @"
UPDATE ScreenConfig
SET
AdminName=@a,
Password=@p,
Title=@t
WHERE Id=1;";

        cmd.Parameters.AddWithValue("@a", config.AdminName);
        cmd.Parameters.AddWithValue("@p", config.Password);
        cmd.Parameters.AddWithValue("@t", config.Title);

        cmd.ExecuteNonQuery();
    }
}