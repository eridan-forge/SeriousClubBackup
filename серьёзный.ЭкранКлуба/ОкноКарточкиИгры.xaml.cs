using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using серьёзный.Модели;

namespace серьёзный.ЭкранКлуба;

public partial class ОкноКарточкиИгры : Window
{
    public event Action<Игра>? Играть;
    public event Action<Игра>? ИзбранноеПереключено;
    public event Action<Игра>? ИграСкрыта;

    public event Action<Игра>? ПодробнееЗапрошено;

    private Игра игра;

    public ОкноКарточкиИгры(Игра игра, bool избранное)
    {
        InitializeComponent();

        this.игра = игра;

        ЗаполнитьСодержимое(игра, избранное);

        Loaded += ПриЗагрузке_АнимацияВУгол;
    }

    // Вызывается вместо создания второго окна поверх первого — окно уже
    // выехано и видно, просто плавно подменяем его карточку.
    public void ПоказатьДругую(Игра новаяИгра, bool избранное)
    {
        игра = новаяИгра;

        var затухание = new DoubleAnimation(1, 0.12, TimeSpan.FromMilliseconds(90))
        {
            AutoReverse = true
        };

        затухание.Completed += (_, _) => ЗаполнитьСодержимое(новаяИгра, избранное);

        BeginAnimation(OpacityProperty, затухание);
    }

    private void ЗаполнитьСодержимое(Игра игра, bool избранное)
    {
        Название.Text = игра.Название;
        Категория.Text = игра.Категория;

        КнопкаИзбранноеПревью.Content = избранное ? "★" : "☆";

        if (!string.IsNullOrWhiteSpace(игра.Обложка) && File.Exists(игра.Обложка))
        {
            var картинка = new BitmapImage();

            картинка.BeginInit();
            картинка.CacheOption = BitmapCacheOption.OnLoad;
            картинка.UriSource = new Uri(игра.Обложка);
            картинка.EndInit();
            картинка.Freeze();

            Обложка.Source = картинка;
            Обложка.Visibility = Visibility.Visible;
            Заглушка.Visibility = Visibility.Collapsed;
        }
        else
        {
            Обложка.Source = null;
            Обложка.Visibility = Visibility.Collapsed;
            Заглушка.Visibility = Visibility.Visible;
        }
    }

    private void ПриЗагрузке_АнимацияВУгол(object sender, RoutedEventArgs e)
    {
        var область = SystemParameters.WorkArea;

        var финишныйLeft = область.Right - Width - 30;

        Top = область.Bottom - Height - 30;
        Left = область.Right;

        var анимация = new DoubleAnimation(Left, финишныйLeft, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        BeginAnimation(LeftProperty, анимация);
    }

    private void Закрыть_Click(object sender, RoutedEventArgs e)
    {
        var область = SystemParameters.WorkArea;

        var анимация = new DoubleAnimation(Left, область.Right, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        анимация.Completed += (_, _) => Close();

        BeginAnimation(LeftProperty, анимация);
    }

    private void Играть_Click(object sender, RoutedEventArgs e)
    {
        Играть?.Invoke(игра);
        Закрыть_Click(sender, e);
    }

    private void Избранное_Click(object sender, RoutedEventArgs e)
    {
        ИзбранноеПереключено?.Invoke(игра);

        КнопкаИзбранноеПревью.Content =
            (string)КнопкаИзбранноеПревью.Content == "★" ? "☆" : "★";
    }

    private void Скрыть_Click(object sender, RoutedEventArgs e)
    {
        ИграСкрыта?.Invoke(игра);
        Закрыть_Click(sender, e);
    }

    private void Подробнее_Click(object sender, RoutedEventArgs e)
    {
        ПодробнееЗапрошено?.Invoke(игра);
        Закрыть_Click(sender, e);
    }
}