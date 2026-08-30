using серьёзный.ЭкранКлуба.Модели;

namespace серьёзный.ЭкранКлуба.Сервисы;

public static class StateService
{
    public static State Загрузить()
    {
        using var db =
            СервисБазыЭкрана001.Открыть();

        using var cmd = db.CreateCommand();

        cmd.CommandText =
            "SELECT Locked, PcId FROM ScreenState WHERE Id=1;";

        using var r = cmd.ExecuteReader();

        r.Read();

        return new State
        {
            Locked = r.GetInt32(0) == 1,
            PcId = r.GetInt32(1)
        };
    }

    public static void Сохранить(
        State state)
    {
        using var db =
            СервисБазыЭкрана001.Открыть();

        using var cmd = db.CreateCommand();

        cmd.CommandText = @"
UPDATE ScreenState
SET
Locked=@l,
PcId=@id
WHERE Id=1;";

        cmd.Parameters.AddWithValue("@l", state.Locked ? 1 : 0);
        cmd.Parameters.AddWithValue("@id", state.PcId);

        cmd.ExecuteNonQuery();
    }
}