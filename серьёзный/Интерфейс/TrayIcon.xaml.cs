using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace серьёзный.Интерфейс;

public partial class TrayIcon : TaskbarIcon
{
    public TrayIcon()
    {
        InitializeComponent();

        TrayMouseDoubleClick += (_, _) =>
        {
            ПоказатьОкно();
        };
    }

    private void Открыть_Click(
        object sender,
        RoutedEventArgs e)
    {
        ПоказатьОкно();
    }

    private void Выход_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is MainWindow окно)
            окно.РазрешитьПолныйВыход();

        Application.Current.Shutdown();
    }

    private void ПоказатьОкно()
    {
        if (Application.Current.MainWindow is not MainWindow окно)
            return;

        окно.Show();
        окно.WindowState = WindowState.Normal;
        окно.Activate();
    }
}