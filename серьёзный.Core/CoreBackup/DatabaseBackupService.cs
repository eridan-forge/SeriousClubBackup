using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace серьёзный.Core.CoreBackup;

// Резервное копирование SeriousClub.db. Используется штатный SQLite
// backup API (BackupDatabase), а не File.Copy — он корректно захватывает
// данные из WAL-файла, даже если в этот момент кто-то параллельно пишет.
public class DatabaseBackupService
{
    private const int МаксимумКопий = 30;

    private readonly string исходнаяБаза =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private readonly string папкаКопий =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "Backups");

    private readonly string путьЛога =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "logs",
            "backup.log");

    // Возвращает путь к созданному файлу копии, либо null при ошибке.
    public string? СоздатьРезервнуюКопию()
    {
        try
        {
            if (!File.Exists(исходнаяБаза))
            {
                ЗаписатьЛог("Пропущено: файл базы данных ещё не создан.");
                return null;
            }

            Directory.CreateDirectory(папкаКопий);

            var имяФайла =
                $"SeriousClub-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db";

            var путьКопии =
                Path.Combine(папкаКопий, имяФайла);

            using (var источник = new SqliteConnection($"Data Source={исходнаяБаза}"))
            using (var назначение = new SqliteConnection($"Data Source={путьКопии}"))
            {
                источник.Open();
                назначение.Open();

                using (var pragma = источник.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA busy_timeout=5000;";
                    pragma.ExecuteNonQuery();
                }

                источник.BackupDatabase(назначение);
            }

            var манифест =
                new BackupManifest
                {
                    Created = DateTime.Now,
                    Version = "1.0",
                    ComputerName = Environment.MachineName
                };

            File.WriteAllText(
                путьКопии + ".json",
                JsonSerializer.Serialize(
                    манифест,
                    new JsonSerializerOptions { WriteIndented = true }));

            ОчиститьСтарыеКопии();

            ЗаписатьЛог($"Резервная копия создана: {путьКопии}");

            return путьКопии;
        }
        catch (Exception ошибка)
        {
            ЗаписатьЛог("ОШИБКА резервного копирования: " + ошибка);
            return null;
        }
    }

    // Хранит только последние МаксимумКопий файлов — папка с копиями
    // никогда не растёт бесконечно, даже спустя годы работы.
    private void ОчиститьСтарыеКопии()
    {
        try
        {
            List<string> файлы =
                Directory.GetFiles(папкаКопий, "SeriousClub-*.db")
                    .OrderByDescending(File.GetCreationTimeUtc)
                    .ToList();

            foreach (var лишний in файлы.Skip(МаксимумКопий))
            {
                try
                {
                    File.Delete(лишний);

                    var манифест = лишний + ".json";

                    if (File.Exists(манифест))
                        File.Delete(манифест);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private void ЗаписатьЛог(string текст)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(путьЛога)!);

            File.AppendAllText(
                путьЛога,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {текст}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}