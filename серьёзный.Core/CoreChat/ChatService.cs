using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using серьёзный.Core.CoreDb;

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

        // ВАЖНО: таблица называется DirectMessages, а не ChatMessages.
        // Имя ChatMessages уже занято другой, несовместимой схемой
        // в серьёзный.Сервисы.СервисЧата (PC-чат в главном окне
        // админки), из-за чего личный чат падал с
        // "SQLite Error 20: datatype mismatch" при попытке вставить
        // GUID в колонку с типом INTEGER PRIMARY KEY.
        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS DirectMessages
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
    }

    private SqliteConnection Open() => SqliteDb.Open();

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
        FROM DirectMessages
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
        FROM DirectMessages
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
        UPDATE DirectMessages
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
        INSERT INTO DirectMessages
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
        INSERT INTO DirectMessages
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