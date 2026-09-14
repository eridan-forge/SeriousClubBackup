using System.Windows;

namespace серьёзный.ЭкранКлуба;

public partial class ИнфоОкно : Window
{
    public ИнфоОкно(string сообщение, string заголовок = "Уведомление")
    {
        InitializeComponent();

        ТекстЗаголовок.Text = заголовок;
        ТекстСообщения.Text = сообщение;
    }

    private void Закрыть_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}