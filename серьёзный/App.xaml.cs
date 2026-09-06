using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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

        серьёзный.CrystalUI.CustomCursor.КурсорОверлей.Запустить();

        // Класс-обработчик срабатывает для КАЖДОГО окна в приложении —
        // MainWindow и всех диалогов — без единой правки в их XAML.
        // Раньше системный курсор прятался только внутри MainWindow;
        // стоило открыть любое другое окно — там сразу показывалась
        // обычная стрелка Windows поверх нашего затемнённого фона.
        EventManager.RegisterClassHandler(
               typeof(Window),
                Window.LoadedEvent,
                 new RoutedEventHandler((sender, _) =>
                 {
                     if (sender is Window окно)
                         окно.Cursor = Cursors.None;
                 }));

        // ContextMenu и ToolTip рендерятся в отдельном Popup-окне, а не

        // внутри дерева родительского Window — предыдущий хендлер их

        // не касался, курсор белой стрелкой выглядывал над контекстным

        // меню карточки ПК и над подсказками.
        EventManager.RegisterClassHandler(
              typeof(ContextMenu),
               ContextMenu.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    if (sender is ContextMenu меню)
                        меню.Cursor = Cursors.None;
                }));


        EventManager.RegisterClassHandler(
                 typeof(ToolTip),
                  ToolTip.LoadedEvent,
 new RoutedEventHandler((sender, _) =>
 {
     if (sender is ToolTip подсказка)
         подсказка.Cursor = Cursors.None;
 }));





        Exit += (_, _) => серьёзный.CrystalUI.CustomCursor.КурсорОверлей.Остановить();


        DispatcherUnhandledException +=
            (_, ошибка) =>
            {
                ЗаписатьЛог(
                    "DispatcherUnhandledException: " +
                    ошибка.Exception);

                MessageBox.Show(
                    "Произошла ошибка, но программа продолжит работу.\n\n" +
                    ошибка.Exception.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                ошибка.Handled = true;
            };

        AppDomain.CurrentDomain.UnhandledException +=
            (_, ошибка) =>
            {
                ЗаписатьЛог(
                    "FATAL: " +
                    ошибка.ExceptionObject);
            };

        TaskScheduler.UnobservedTaskException +=
            (_, ошибка) =>
            {
                ЗаписатьЛог(
                    "UNOBSERVED TASK: " +
                    ошибка.Exception);

                ошибка.SetObserved();
            };

        fileServer.Start();
        worker.Start();

        ПодключитьУстановщик();
    }

    private static void ЗаписатьЛог(string текст)
    {
        try
        {
            var папка =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub",
                    "logs");

            Directory.CreateDirectory(папка);

            File.AppendAllText(
                Path.Combine(папка, "admin-crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {текст}{Environment.NewLine}");
        }
        catch
        {
        }
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