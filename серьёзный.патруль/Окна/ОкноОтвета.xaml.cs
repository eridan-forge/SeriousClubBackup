using System.Windows;

namespace серьёзный.Патруль.Окна;

public partial class ОкноОтвета : Window
{
    public string Текст =>
        Поле.Text.Trim();

    public ОкноОтвета()
    {
        InitializeComponent();

        Loaded += (_, _) => Поле.Focus();
    }

    private void Отправить_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}