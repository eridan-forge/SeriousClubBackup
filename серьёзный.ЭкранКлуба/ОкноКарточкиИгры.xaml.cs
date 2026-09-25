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

        Loaded += ПриЗагрузке_АнимацияВУгол;
    }

    private void ПриЗагрузке_АнимацияВУгол(object sender, RoutedEventArgs e)
    {
        var область = SystemParameters.WorkArea;

       var финишныйLeft = область.Right - Width - 30;

        Top = область.Bottom - Height - 30;
        Left = область.Right; // старт за пределами экрана справа

        var анимация = new System.Windows.Media.Animation.DoubleAnimation(
Left, финишныйLeft, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new System.Windows.Media.Animation.CubicEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            }
        };

BeginAnimation(LeftProperty, анимация);
    }

    private void Закрыть_Click(object sender, RoutedEventArgs e) => Close();

    private void Играть_Click(object sender, RoutedEventArgs e)
    {
        Играть?.Invoke(игра);
        Close();
    }
}