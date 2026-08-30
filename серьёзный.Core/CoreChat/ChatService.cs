using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;

namespace серьёзный.Core.CoreChat;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid From { get; set; }

    public Guid To { get; set; }

    public string Text { get; set; } = "";

    public string? VoiceFile { get; set; }

    public bool IsVoice =>
        !string.IsNullOrWhiteSpace(VoiceFile);

    public bool Read { get; set; }

    public DateTime Time { get; set; } = DateTime.Now;
}

public static class ChatLiveEvents
{
    public static event Action<ChatMessage>? MessageReceived;

    public static void Raise(ChatMessage msg)
    {
        MessageReceived?.Invoke(msg);
    }
}

public class ChatService
{
    private readonly string dbPath =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public ChatService()
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(dbPath)!);

        using var con =
            new SqliteConnection(
                $"Data Source={dbPath}");

        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS ChatMessages
        (
            Id TEXT PRIMARY KEY,
            FromId TEXT NOT NULL,
            ToId TEXT NOT NULL,
            Text TEXT NOT NULL,
            VoiceFile TEXT,
            Read INTEGER NOT NULL,
            Time TEXT NOT NULL
        );
        """;

        cmd.ExecuteNonQuery();

        // =========================
        // Миграция старой базы
        // =========================

        cmd.CommandText = "PRAGMA table_info(ChatMessages);";

        using (var reader = cmd.ExecuteReader())
        {
            bool hasFromId = false;
            bool hasToId = false;
            bool hasText = false;
            bool hasVoiceFile = false;
            bool hasRead = false;
            bool hasTime = false;

            bool hasOldFrom = false;
            bool hasOldTo = false;

            while (reader.Read())
            {
                var name = reader.GetString(1);

                if (name == "FromId") hasFromId = true;
                if (name == "ToId") hasToId = true;
                if (name == "Text") hasText = true;
                if (name == "VoiceFile") hasVoiceFile = true;
                if (name == "Read") hasRead = true;
                if (name == "Time") hasTime = true;

                if (name == "From") hasOldFrom = true;
                if (name == "To") hasOldTo = true;
            }

            reader.Close();

            if (!hasFromId)
            {
                cmd.CommandText = "ALTER TABLE ChatMessages ADD COLUMN FromId TEXT;";
                cmd.ExecuteNonQuery();
            }

            if (!hasToId)
            {
                cmd.CommandText = "ALTER TABLE ChatMessages ADD COLUMN ToId TEXT;";
                cmd.ExecuteNonQuery();
            }

            if (!hasText)
            {
                cmd.CommandText = "ALTER TABLE ChatMessages ADD COLUMN Text TEXT NOT NULL DEFAULT '';";
                cmd.ExecuteNonQuery();
            }

            if (!hasVoiceFile)
            {
                cmd.CommandText = "ALTER TABLE ChatMessages ADD COLUMN VoiceFile TEXT;";
                cmd.ExecuteNonQuery();
            }

            if (!hasRead)
            {
                cmd.CommandText = "ALTER TABLE ChatMessages ADD COLUMN Read INTEGER NOT NULL DEFAULT 0;";
                cmd.ExecuteNonQuery();
            }

            if (!hasTime)
            {
                cmd.CommandText = "ALTER TABLE ChatMessages ADD COLUMN Time TEXT;";
                cmd.ExecuteNonQuery();
            }

            if (hasOldFrom)
            {
                cmd.CommandText = "UPDATE ChatMessages SET FromId=\"From\" WHERE FromId IS NULL;";
                cmd.ExecuteNonQuery();
            }

            if (hasOldTo)
            {
                cmd.CommandText = "UPDATE ChatMessages SET ToId=\"To\" WHERE ToId IS NULL;";
                cmd.ExecuteNonQuery();
            }
        }
    }

    private SqliteConnection Open()
    {
        var con =
            new SqliteConnection(
                $"Data Source={dbPath}");

        con.Open();

        return con;
    }

    // ==========================================
    // Диалог
    // ==========================================

    public IReadOnlyList<ChatMessage> Get(
        Guid a,
        Guid b)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        SELECT Id,FromId,ToId,Text,VoiceFile,Read,Time
        FROM ChatMessages
        WHERE
        (FromId=$a AND ToId=$b)
        OR
        (FromId=$b AND ToId=$a)
        ORDER BY Time;
        """;

        cmd.Parameters.AddWithValue("$a", a.ToString());
        cmd.Parameters.AddWithValue("$b", b.ToString());

        var list = new List<ChatMessage>();

        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            list.Add(
                new ChatMessage
                {
                    Id =
                        Guid.Parse(r.GetString(0)),

                    From =
                        Guid.Parse(r.GetString(1)),

                    To =
                        Guid.Parse(r.GetString(2)),

                    Text =
                        r.GetString(3),

                    VoiceFile =
                        r.IsDBNull(4)
                            ? null
                            : r.GetString(4),

                    Read =
                        r.GetInt32(5) == 1,

                    Time =
                        DateTime.Parse(r.GetString(6))
                });
        }

        return list;
    }

    // ==========================================
    // Непрочитанные
    // ==========================================

    public int Unread(Guid me)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        SELECT COUNT(*)
        FROM ChatMessages
        WHERE ToId=$id
        AND Read=0;
        """;

        cmd.Parameters.AddWithValue("$id", me.ToString());

        return Convert.ToInt32(
            cmd.ExecuteScalar());
    }

    // ==========================================
    // Прочитать
    // ==========================================

    public void MarkRead(
        Guid me,
        Guid friend)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        UPDATE ChatMessages
        SET Read=1
        WHERE ToId=$me
        AND FromId=$friend;
        """;

        cmd.Parameters.AddWithValue("$me", me.ToString());
        cmd.Parameters.AddWithValue("$friend", friend.ToString());

        cmd.ExecuteNonQuery();
    }

    // ==========================================
    // Отправить текст
    // ==========================================

    public void Send(
        Guid from,
        Guid to,
        string text)
    {
        var msg =
            new ChatMessage
            {
                From = from,
                To = to,
                Text = text,
                Time = DateTime.Now
            };

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO ChatMessages
        (
            Id,
            FromId,
            ToId,
            Text,
            VoiceFile,
            Read,
            Time
        )
        VALUES
        (
            $id,
            $from,
            $to,
            $text,
            NULL,
            0,
            $time
        );
        """;

        cmd.Parameters.AddWithValue("$id", msg.Id.ToString());
        cmd.Parameters.AddWithValue("$from", msg.From.ToString());
        cmd.Parameters.AddWithValue("$to", msg.To.ToString());
        cmd.Parameters.AddWithValue("$text", msg.Text);
        cmd.Parameters.AddWithValue("$time", msg.Time.ToString("O"));

        cmd.ExecuteNonQuery();

        ChatLiveEvents.Raise(msg);
    }

    // ==========================================
    // Отправить голосовое
    // ==========================================

    public void SendVoice(
        Guid from,
        Guid to,
        string filePath)
    {
        var msg =
            new ChatMessage
            {
                From = from,
                To = to,
                VoiceFile = filePath,
                Time = DateTime.Now
            };

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO ChatMessages
        (
            Id,
            FromId,
            ToId,
            Text,
            VoiceFile,
            Read,
            Time
        )
        VALUES
        (
            $id,
            $from,
            $to,
            '',
            $voice,
            0,
            $time
        );
        """;

        cmd.Parameters.AddWithValue("$id", msg.Id.ToString());
        cmd.Parameters.AddWithValue("$from", msg.From.ToString());
        cmd.Parameters.AddWithValue("$to", msg.To.ToString());
        cmd.Parameters.AddWithValue("$voice", msg.VoiceFile);
        cmd.Parameters.AddWithValue("$time", msg.Time.ToString("O"));

        cmd.ExecuteNonQuery();

        ChatLiveEvents.Raise(msg);
    }

    // ==========================================
    // Экспорт истории (на будущее)
    // ==========================================

    public string ExportJson(
        Guid a,
        Guid b)
    {
        return JsonSerializer.Serialize(
            Get(a, b),
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }
}