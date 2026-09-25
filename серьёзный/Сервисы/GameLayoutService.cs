using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using серьёзный.Модели;

namespace серьёзный.Сервисы
{
    public class GameLayoutService
    {
        private string FilePath(int pc)
        {
            return Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub",
                "Layouts",
                $"pc-{pc}.json");
        }

        public CardLayout Load(int pc)
        {
            var file = FilePath(pc);

            Directory.CreateDirectory(
                Path.GetDirectoryName(file)!);

            if (!File.Exists(file))
                return new CardLayout { PcId = pc };

            try
            {
                return JsonSerializer.Deserialize<CardLayout>(
                    File.ReadAllText(file))
                    ?? new CardLayout { PcId = pc };
            }
            catch
            {
                return new CardLayout { PcId = pc };
            }
        }

        public void Save(CardLayout layout)
        {
            var file = FilePath(layout.PcId);

            Directory.CreateDirectory(
                Path.GetDirectoryName(file)!);

            File.WriteAllText(
                file,
                JsonSerializer.Serialize(
                    layout,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }
    }
}