using System.Windows;
using серьёзный.Core.CoreAudit;

namespace серьёзный.ЭкранКлуба;

public partial class ОкноИсторииОбслуживания : Window
{
    private readonly AdminActionLogService лог = new();

    public ОкноИсторииОбслуживания()
    {
        InitializeComponent();

        Loaded += (_, _) => Обновить();
    }

    private void Обновить()
    {
        Таблица.ItemsSource = null;
        Таблица.ItemsSource = лог.GetRecent(300);
    }

    private void Обновить_Click(object sender, RoutedEventArgs e) => Обновить();

    private void Закрыть_Click(object sender, RoutedEventArgs e) => Close();
}