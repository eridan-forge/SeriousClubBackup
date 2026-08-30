using System;
using System.Runtime.Versioning;
using System.Windows;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;
using серьёзный.Модели;
using серьёзный.Сервисы;
using серьёзный.Уведомления;

namespace серьёзный;

[SupportedOSPlatform("windows")]
public partial class App : Application
{
    private readonly FileServer fileServer = new();
    private readonly TransferWorker worker = new();

    private readonly ShopToastManager shopToast =
    new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        fileServer.Start();
        worker.Start();

        ПодключитьУстановщик();
    }

    private void ПодключитьУстановщик()
    {
        GameInstallEvent.InstallRequested += (pcId, game) =>
        {
            var сервис = new СервисИгр();

            Guid gameId =
                Guid.TryParse(game.Id, out var parsed)
                    ? parsed
                    : Guid.NewGuid();

            bool существует =
                сервис.ПолучитьИгры(pcId)
                      .Exists(x => x.Id == gameId);

            if (существует)
                return;

            сервис.Добавить(
                pcId,
                new Игра
                {
                    Id = gameId,
                    Название = game.Name,
                    Категория = game.Category,
                    Описание = game.Description,
                    Путь = game.Path,
                    Обложка = game.Image,
                    Скрыта = game.Hidden
                });
        };
    }
}