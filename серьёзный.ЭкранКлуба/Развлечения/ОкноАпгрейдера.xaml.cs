using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace серьёзный.ЭкранКлуба.Развлечения;

public partial class ОкноАпгрейдера : Window
{
    private static readonly Dictionary<int, double> ШансПоМножителю = new()
    {
        { 2, 45 }, { 3, 30 }, { 4, 22 }, { 5, 17 }, { 6, 14 }, { 7, 12 }
    };

    private readonly Random random = new();

    private int выбранныйМножитель = 2;
    private bool крутится;

    public ОкноАпгрейдера()
    {
        InitializeComponent();

        СписокМоихПредметов.ItemsSource = ТестовыйИнвентарь.Получить();
        СписокМоихПредметов.SelectedIndex = 0;

        СписокЦелевыхПредметов.ItemsSource = ТестовыйИнвентарь.ПолучитьЦели();
        СписокЦелевыхПредметов.SelectedIndex = 0;

        ПостроитьКнопкиМножителя();
        ОбновитьКруг();
    }

    private void ПостроитьКнопкиМножителя()
    {
        ПанельМножителей.Children.Clear();

        foreach (var множитель in ШансПоМножителю.Keys)
        {
            var кнопка = new Button
            {
                Content = $"x{множитель}",
                Width = 56,
                Height = 40,
                Margin = new Thickness(4, 0, 0, 0),
                Tag = множитель == выбранныйМножитель ? "Активна" : null,
                Style = (Style)FindResource("ПодвкладкаКнопка")
            };

            кнопка.Click += (_, _) =>
            {
                выбранныйМножитель = множитель;
                ПостроитьКнопкиМножителя();
                ОбновитьКруг();
            };

            ПанельМножителей.Children.Add(кнопка);
        }
    }

    private void ВыборПредмета_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (СписокМоихПредметов.SelectedItem is ПредметКейса предмет)
            ОтрисоватьКарточку(КарточкаМоегоПредмета, предмет);
    }

    private void ВыборЦели_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (СписокЦелевыхПредметов.SelectedItem is ПредметКейса предмет)
            ОтрисоватьКарточку(КарточкаЦели, предмет);
    }

    private void ОтрисоватьКарточку(Border карточка, ПредметКейса предмет)
    {
        var цвет = РедкостьСтиль.Цвет(предмет.Редкость);

        карточка.Background = new SolidColorBrush(Color.FromArgb(40, цвет.R, цвет.G, цвет.B));
        карточка.BorderBrush = new SolidColorBrush(цвет);
        карточка.BorderThickness = new Thickness(2);

        карточка.Child = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = предмет.Иконка, FontSize = 40, HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock
                {
                    Text = предмет.Название,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                }
            }
        };
    }

    private void ОбновитьКруг()
    {
        double шанс = ШансПоМножителю[выбранныйМножитель];

        ТекстШанс.Text = $"{шанс:0}%";

        var цветЗоны = ЦветПоШансу(шанс);
        double уголЗоны = шанс / 100.0 * 360.0;

        ХолстКруга.Children.Clear();
        ХолстКруга.Children.Add(СоздатьСектор(160, 160, 155, 0, уголЗоны, new SolidColorBrush(цветЗоны)));
        ХолстКруга.Children.Add(СоздатьСектор(160, 160, 155, уголЗоны, 360, new SolidColorBrush(Color.FromRgb(0x24, 0x14, 0x19))));
    }

    private static Color ЦветПоШансу(double шанс)
    {
        if (шанс >= 40) return Color.FromRgb(0x22, 0xC5, 0x5E);
        if (шанс >= 20) return Color.FromRgb(0xFB, 0xBF, 0x24);
        return Color.FromRgb(0xEF, 0x44, 0x44);
    }

    private static Path СоздатьСектор(double centerX, double centerY, double radius, double startDeg, double endDeg, Brush заливка)
    {
        Point ToPoint(double deg)
        {
            double rad = (deg - 90) * Math.PI / 180.0;
            return new Point(centerX + radius * Math.Cos(rad), centerY + radius * Math.Sin(rad));
        }

        var figure = new PathFigure { StartPoint = new Point(centerX, centerY), IsClosed = true };
        figure.Segments.Add(new LineSegment(ToPoint(startDeg), true));
        figure.Segments.Add(new ArcSegment(ToPoint(endDeg), new Size(radius, radius), 0, endDeg - startDeg > 180, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        return new Path { Data = geometry, Fill = заливка };
    }

    private void Апгрейд_Click(object sender, RoutedEventArgs e)
    {
        if (крутится) return;

        if (СписокМоихПредметов.SelectedItem is not ПредметКейса мой || СписокЦелевыхПредметов.SelectedItem is not ПредметКейса цель)
        {
            MessageBox.Show("Выберите свой предмет и то, что хотите получить.");
            return;
        }

        крутится = true;
        КнопкаАпгрейд.IsEnabled = false;

        double шанс = ШансПоМножителю[выбранныйМножитель];
        double уголЗоны = шанс / 100.0 * 360.0;

        bool успех = random.NextDouble() * 100.0 < шанс;

        double финальныйУгол = успех
            ? random.NextDouble() * уголЗоны
            : уголЗоны + random.NextDouble() * (360 - уголЗоны);

        double текущийУгол = ВращениеСтрелки.Angle % 360;
        double итоговыйУгол = ВращениеСтрелки.Angle + 5 * 360 + ((360 - текущийУгол) + финальныйУгол);

        var анимация = new DoubleAnimation(ВращениеСтрелки.Angle, итоговыйУгол, TimeSpan.FromSeconds(4.5))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        анимация.Completed += (_, _) =>
        {
            ВращениеСтрелки.Angle %= 360;

            MessageBox.Show(
                успех
                    ? $"🎉 Апгрейд успешен! Получен предмет: {цель.Название}"
                    : $"😔 Не повезло. Предмет «{мой.Название}» потерян.",
                "Апгрейдер");

            крутится = false;
            КнопкаАпгрейд.IsEnabled = true;
        };

        ВращениеСтрелки.BeginAnimation(RotateTransform.AngleProperty, анимация);
    }
}