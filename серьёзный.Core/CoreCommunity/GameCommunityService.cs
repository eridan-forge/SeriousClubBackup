using Microsoft.Data.Sqlite;
using System.IO;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreCommunity;

public class GameRatingSummary
{
    public double Average { get; set; }
    public int Count { get; set; }
    public int MyRating { get; set; }
}

public class GameReviewRecord
{
    public long Id { get; set; }
    public string PlayerName { get; set; } = "";
    public int Stars { get; set; }
    public string Text { get; set; } = "";
    public DateTime Time { get; set; }
}

public class GameHelpQuestionRecord
{
    public long Id { get; set; }
    public string GameName { get; set; } = "";
    public string Question { get; set; } = "";
    public string? Answer { get; set; }
    public bool Answered => !string.IsNullOrWhiteSpace(Answer);
    public string AskedBy { get; set; } = "";
    public DateTime Time { get; set; }
}

// Рейтинги/отзывы/вопросы об играх — общие для ВСЕХ ПК клуба, поэтому
// ключом служит НАЗВАНИЕ игры (Игра.Название), а не Guid конкретной
// карточки каталога: у каждого ПК свой отдельный JSON-каталог игр
// (СервисИгр), и одна и та же игра, добавленная на разных ПК,
// получает разные Guid — по ним нельзя было бы объединить отзывы с
// разных ПК в один рейтинг "среди игроков сервера".
public class GameCommunityService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public GameCommunityService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS GameRatings(
            GameKey TEXT NOT NULL,
            GameName TEXT NOT NULL,
            PlayerId TEXT NOT NULL,
            Stars INTEGER NOT NULL,
            Time TEXT NOT NULL,
            PRIMARY KEY(GameKey, PlayerId)
        );

        CREATE TABLE IF NOT EXISTS GameReviews(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            GameKey TEXT NOT NULL,
            GameName TEXT NOT NULL,
            PlayerId TEXT NOT NULL,
            PlayerName TEXT NOT NULL,
            Text TEXT NOT NULL,
            Stars INTEGER NOT NULL DEFAULT 0,
            Time TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS GameHelpQuestions(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            GameKey TEXT NOT NULL,
            GameName TEXT NOT NULL,
            PlayerId TEXT NOT NULL,
            AskedBy TEXT NOT NULL,
            Question TEXT NOT NULL,
            Answer TEXT,
            AdminName TEXT,
            Time TEXT NOT NULL,
            AnsweredTime TEXT
        );
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open() => SqliteDb.Open();

    private static string Ключ(string gameName) =>
        (gameName ?? "").Trim().ToLowerInvariant();

    // =====================================================
    // РЕЙТИНГ
    // =====================================================

    public GameRatingSummary GetRating(string gameName, Guid playerId)
    {
        var key = Ключ(gameName);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "SELECT AVG(Stars), COUNT(*) FROM GameRatings WHERE GameKey=$k;";
        cmd.Parameters.AddWithValue("$k", key);

        double average = 0;
        int count = 0;

        using (var r = cmd.ExecuteReader())
        {
            if (r.Read() && !r.IsDBNull(0))
            {
                average = r.GetDouble(0);
                count = r.GetInt32(1);
            }
        }

        using var mineCmd = con.CreateCommand();

        mineCmd.CommandText = "SELECT Stars FROM GameRatings WHERE GameKey=$k AND PlayerId=$p;";
        mineCmd.Parameters.AddWithValue("$k", key);
        mineCmd.Parameters.AddWithValue("$p", playerId.ToString());

        var mineValue = mineCmd.ExecuteScalar();

        return new GameRatingSummary
        {
            Average = average,
            Count = count,
            MyRating = mineValue == null ? 0 : Convert.ToInt32(mineValue)
        };
    }

    public void SetRating(string gameName, Guid playerId, int stars)
    {
        stars = Math.Clamp(stars, 1, 5);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO GameRatings(GameKey, GameName, PlayerId, Stars, Time)
        VALUES($k,$n,$p,$s,$t)
        ON CONFLICT(GameKey, PlayerId) DO UPDATE SET
            Stars=$s, Time=$t, GameName=$n;
        """;

        cmd.Parameters.AddWithValue("$k", Ключ(gameName));
        cmd.Parameters.AddWithValue("$n", gameName);
        cmd.Parameters.AddWithValue("$p", playerId.ToString());
        cmd.Parameters.AddWithValue("$s", stars);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    // =====================================================
    // ОТЗЫВЫ
    // =====================================================

    public (List<GameReviewRecord> Reviews, int Count) GetReviews(string gameName, int take = 50)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, PlayerName, Stars, Text, Time FROM GameReviews " +
            "WHERE GameKey=$k ORDER BY Id DESC LIMIT $take;";

        cmd.Parameters.AddWithValue("$k", Ключ(gameName));
        cmd.Parameters.AddWithValue("$take", take);

        var list = new List<GameReviewRecord>();

        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                list.Add(new GameReviewRecord
                {
                    Id = r.GetInt64(0),
                    PlayerName = r.GetString(1),
                    Stars = r.GetInt32(2),
                    Text = r.GetString(3),
                    Time = DateTime.Parse(r.GetString(4))
                });
            }
        }

        using var countCmd = con.CreateCommand();

        countCmd.CommandText = "SELECT COUNT(*) FROM GameReviews WHERE GameKey=$k;";
        countCmd.Parameters.AddWithValue("$k", Ключ(gameName));

        var count = Convert.ToInt32(countCmd.ExecuteScalar());

        return (list, count);
    }

    public void AddReview(string gameName, Guid playerId, string playerName, string text, int stars)
    {
        text = (text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (text.Length > 1000)
            text = text.Substring(0, 1000);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO GameReviews(GameKey, GameName, PlayerId, PlayerName, Text, Stars, Time)
        VALUES($k,$n,$p,$pn,$t,$s,$tm);
        """;

        cmd.Parameters.AddWithValue("$k", Ключ(gameName));
        cmd.Parameters.AddWithValue("$n", gameName);
        cmd.Parameters.AddWithValue("$p", playerId.ToString());
        cmd.Parameters.AddWithValue("$pn", playerName);
        cmd.Parameters.AddWithValue("$t", text);
        cmd.Parameters.AddWithValue("$s", Math.Clamp(stars, 0, 5));
        cmd.Parameters.AddWithValue("$tm", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    // =====================================================
    // ПОМОЩЬ / ВОПРОСЫ
    // =====================================================

    public List<GameHelpQuestionRecord> GetHelp(string gameName, int take = 50)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, GameName, Question, Answer, AskedBy, Time FROM GameHelpQuestions " +
            "WHERE GameKey=$k ORDER BY (Answer IS NOT NULL) DESC, Id DESC LIMIT $take;";

        cmd.Parameters.AddWithValue("$k", Ключ(gameName));
        cmd.Parameters.AddWithValue("$take", take);

        var list = new List<GameHelpQuestionRecord>();

        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            list.Add(new GameHelpQuestionRecord
            {
                Id = r.GetInt64(0),
                GameName = r.GetString(1),
                Question = r.GetString(2),
                Answer = r.IsDBNull(3) ? null : r.GetString(3),
                AskedBy = r.GetString(4),
                Time = DateTime.Parse(r.GetString(5))
            });
        }

        return list;
    }

    public void AskQuestion(string gameName, Guid playerId, string playerName, string question)
    {
        question = (question ?? "").Trim();

        if (string.IsNullOrWhiteSpace(question))
            return;

        if (question.Length > 500)
            question = question.Substring(0, 500);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO GameHelpQuestions(GameKey, GameName, PlayerId, AskedBy, Question, Time)
        VALUES($k,$n,$p,$a,$q,$t);
        """;

        cmd.Parameters.AddWithValue("$k", Ключ(gameName));
        cmd.Parameters.AddWithValue("$n", gameName);
        cmd.Parameters.AddWithValue("$p", playerId.ToString());
        cmd.Parameters.AddWithValue("$a", playerName);
        cmd.Parameters.AddWithValue("$q", question);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    public void AnswerQuestion(long questionId, string answer, string? adminName)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        UPDATE GameHelpQuestions
        SET Answer=$a, AdminName=$n, AnsweredTime=$t
        WHERE Id=$id;
        """;

        cmd.Parameters.AddWithValue("$a", answer);
        cmd.Parameters.AddWithValue("$n", (object?)adminName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$id", questionId);

        cmd.ExecuteNonQuery();
    }

    // Для админ-панели — вопросы по ВСЕМ играм сразу.
    public List<GameHelpQuestionRecord> GetAllQuestions(bool onlyUnanswered)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = onlyUnanswered
            ? "SELECT Id, GameName, Question, Answer, AskedBy, Time FROM GameHelpQuestions " +
              "WHERE Answer IS NULL ORDER BY Id DESC;"
            : "SELECT Id, GameName, Question, Answer, AskedBy, Time FROM GameHelpQuestions " +
              "ORDER BY Id DESC;";

        var list = new List<GameHelpQuestionRecord>();

        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            list.Add(new GameHelpQuestionRecord
            {
                Id = r.GetInt64(0),
                GameName = r.GetString(1),
                Question = r.GetString(2),
                Answer = r.IsDBNull(3) ? null : r.GetString(3),
                AskedBy = r.GetString(4),
                Time = DateTime.Parse(r.GetString(5))
            });
        }

        return list;
    }
}