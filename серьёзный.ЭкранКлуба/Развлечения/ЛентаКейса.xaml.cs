using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace серьёзный.ЭкранКлуба.Развлечения;

public partial class ЛентаКейса : UserControl
{
    private const double ШиринаЯчейки = 130;
    private const int ВсегоЯчеек = 60;
    private const int ИндексОстановки = 52;

    public event Action<ПредметКейса>? Остановилась;

    public ЛентаКейса()
    {
        InitializeComponent();
    }

    public void Запустить(ОпределениеКейса кейс, Random random, bool быстро)
    {
        var победитель = кейс.ВыбратьСлучайный(random);

        Лента.Children.Clear();

        for (int i = 0; i < ВсегоЯчеек; i++)
        {
            var предмет = i == ИндексОстановки ? победитель : кейс.ВыбратьСлучайный(random);
            Лента.Children.Add(СоздатьЯчейку(предмет));
        }

        Лента.Width = ШиринаЯчейки * ВсегоЯчеек;

        double центрПолосы = (!double.IsNaN(Width) && Width > 0 ? Width : (ActualWidth > 0 ? ActualWidth : 640)) / 2.0;

        double целеваяПозиция = -(ИндексОстановки * ШиринаЯчейки + ШиринаЯчейки / 2.0 - центрПолосы);
        целеваяПозиция += (random.NextDouble() - 0.5) * (ШиринаЯчейки * 0.5);

        var длительность = TimeSpan.FromSeconds(быстро ? 2.6 : 5.2);

        var анимация = new DoubleAnimation(0, целеваяПозиция, длительность)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        анимация.Completed += (_, _) =>
        {
            ПодсветитьПобедителя(победитель);
            Остановилась?.Invoke(победитель);
        };

        Сдвиг.BeginAnimation(TranslateTransform.XProperty, анимация);
    }

    private Border СоздатьЯчейку(ПредметКейса предмет)
    {
        var цвет = РедкостьСтиль.Цвет(предмет.Редкость);

        var стек = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        стек.Children.Add(new TextBlock { Text = предмет.Иконка, FontSize = 40, HorizontalAlignment = HorizontalAlignment.Center });

        стек.Children.Add(new TextBlock
        {
            Text = предмет.Название,
            FontSize = 11,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            MaxWidth = 110,
            Margin = new Thickness(0, 6, 0, 0)
        });

        return new Border
        {
            Width = ШиринаЯчейки,
            Height = 150,
            Background = new SolidColorBrush(Color.FromArgb(40, цвет.R, цвет.G, цвет.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(140, цвет.R, цвет.G, цвет.B)),
            BorderThickness = new Thickness(0, 0, 1, 4),
            Child = стек
        };
    }

    private void ПодсветитьПобедителя(ПредметКейса победитель)
    {
        var цвет = РедкостьСтиль.Цвет(победитель.Редкость);

        if (Лента.Children[ИндексОстановки] is Border ячейка)
        {
            ячейка.Effect = new DropShadowEffect
            {
                Color = цвет,
                BlurRadius = 30,
                ShadowDepth = 0,
                Opacity = 0.9
            };
        }
    }
}