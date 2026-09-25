using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using серьёзный.Модели;

namespace серьёзный.ЭкранКлуба;

public partial class ОкноКарточкиИгры : Window
{
    public event Action<Игра>? Играть;

    private readonly Игра игра;

    public ОкноКарточкиИгры(Игра игра)
    {
        InitializeComponent();

        this.игра = игра;

        Название.Text = игра.Название;
        Категория.Text = игра.Категория;

        if (!string.IsNullOrWhiteSpace(игра.Обложка) && File.Exists(игра.Обложка))
        {
            var картинка = new BitmapImage();

            картинка.BeginInit();
            картинка.CacheOption = BitmapCacheOption.OnLoad;
            картинка.UriSource = new Uri(игра.Обложка);
            картинка.EndInit();
            картинка.Freeze();

            Обложка.Source = картинка;
        }
        else
        {
            Обложка.Visibility = Visibility.Collapsed;
            Заглушка.Visibility = Visibility.Visible;
        }
    }

    private void Закрыть_Click(object sender, RoutedEventArgs e) => Close();

    private void Играть_Click(object sender, RoutedEventArgs e)
    {
        Играть?.Invoke(игра);
        Close();
    }
}