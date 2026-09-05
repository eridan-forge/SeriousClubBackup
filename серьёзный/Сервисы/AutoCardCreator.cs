using System;
using System.IO;
using серьёзный.Модели;
using серьёзный.Сервисы.ОпределениеИгр;

namespace серьёзный.Сервисы
{
    public static class AutoCardCreator
    {
        public static Игра Create(string exePath)
        {
            var info =
                GameDetector.Detect(exePath);

            return new Игра
            {
                Id = Guid.NewGuid(),
                Название = info.Name,
                Категория = info.Category,
                Путь = exePath,
                Обложка = "",
                Скрыта = false,
                AppId = info.AppId,
                Launcher = info.Launcher
            };
        }
    }
}