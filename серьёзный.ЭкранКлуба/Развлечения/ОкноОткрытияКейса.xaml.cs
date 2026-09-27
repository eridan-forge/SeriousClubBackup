using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace серьёзный.ЭкранКлуба.Развлечения;

public partial class ОкноОткрытияКейса : Window
{
    private readonly ОпределениеКейса кейс;
    private readonly Random random = new();

    private int выбранноеКоличество = 1;
    private bool быстраяПрокрутка;

    private int количествоЗавершённых;
    private readonly List<ПредметКейса> результаты = new();

    public ОкноОткрытияКейса(ОпределениеКейса кейс)
    {
        InitializeComponent();

        this.кейс = кейс;

        ИконкаКейса.Text = кейс.Иконка;
        НазваниеКейса.Text = кейс.Название;

        ПостроитьКнопкиКоличества();
        ПостроитьШансы();
    }

    private void ПостроитьКнопкиКоличества()
    {
        ПанельКоличества.Children.Clear();

        for (int i = 1; i <= 6; i++)
        {
            var значение = i;

            var кнопка = new Button
            {
                Content = значение.ToString(),
                Width = 42,
                Height = 42,
                Margin = new Thickness(4, 0, 0, 0),
                Tag = значение == выбранноеКоличество ? "Активна" : null,
                Style = (Style)FindResource("ПодвкладкаКнопка")
            };

            кнопка.Click += (_, _) =>
            {
                выбранноеКоличество = значение;
                ПостроитьКнопкиКоличества();
            };

            ПанельКоличества.Children.Add(кнопка);
        }
    }

    private void ПостроитьШансы()
    {
        ПанельШансов.Children.Clear();

        int суммаВесов = кейс.Предметы.Sum(x => x.Вес);

        foreach (var предмет in кейс.Предметы.OrderByDescending(x => x.Редкость))
        {
            var цвет = РедкостьСтиль.Цвет(предмет.Редкость);
            double процент = предмет.Вес * 100.0 / суммаВесов;

            var стек = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 22, 8) };

            стек.Children.Add(new TextBlock { Text = предмет.Иконка, FontSize = 20, Margin = new Thickness(0, 0, 8, 0) });

            var текстовыйСтек = new StackPanel();

            текстовыйСтек.Children.Add(new TextBlock
            {
                Text = предмет.Название,
                Foreground = new SolidColorBrush(цвет),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            });

            текстовыйСтек.Children.Add(new TextBlock { Text = $"{процент:0.##}%", Foreground = Brushes.Gray, FontSize = 11 });

            стек.Children.Add(текстовыйСтек);

            ПанельШансов.Children.Add(стек);
        }
    }

    private void ПереключитьСкорость_Click(object sender, RoutedEventArgs e)
    {
        быстраяПрокрутка = !быстраяПрокрутка;
        КнопкаБыстро.Content = быстраяПрокрутка ? "⚡ Быстрая скорость" : "🐢 Обычная скорость";
        КнопкаБыстро.Tag = быстраяПрокрутка ? "Активна" : null;
    }

    private void Открыть_Click(object sender, RoutedEventArgs e)
    {
        КнопкаОткрыть.IsEnabled = false;

        результаты.Clear();
        количествоЗавершённых = 0;

        ПанельЛент.Children.Clear();
        ПанельРезультата.Visibility = Visibility.Collapsed;
        ПанельЛент.Visibility = Visibility.Visible;

        double ширинаЛенты = выбранноеКоличество switch
        {
            1 => 640,
            2 => 380,
            3 => 260,
            4 => 200,
            5 => 170,
            _ => 150
        };

        for (int i = 0; i < выбранноеКоличество; i++)
        {
            var лента = new ЛентаКейса { Width = ширинаЛенты, Margin = new Thickness(6) };

            лента.Остановилась += предмет =>
            {
                результаты.Add(предмет);
                количествоЗавершённых++;

                if (количествоЗавершённых == выбранноеКоличество)
                    ПоказатьИтог();
            };

            ПанельЛент.Children.Add(лента);

            var задержка = TimeSpan.FromMilliseconds(random.Next(0, 250));

            var t = new DispatcherTimer { Interval = задержка };
            t.Tick += (_, _) =>
            {
                t.Stop();
                лента.Запустить(кейс, random, быстраяПрокрутка);
            };
            t.Start();
        }
    }

    private void ПоказатьИтог()
    {
        ПанельРезультата.Children.Clear();

        foreach (var предмет in результаты)
        {
            var цвет = РедкостьСтиль.Цвет(предмет.Редкость);

            var стек = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            стек.Children.Add(new TextBlock { Text = предмет.Иконка, FontSize = 44, HorizontalAlignment = HorizontalAlignment.Center });
            стек.Children.Add(new TextBlock
            {
                Text = предмет.Название,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });
            стек.Children.Add(new TextBlock
            {
                Text = предмет.Значение,
                Foreground = new SolidColorBrush(цвет),
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });

            ПанельРезультата.Children.Add(new Border
            {
                Width = 140,
                Height = 170,
                Margin = new Thickness(8),
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(40, цвет.R, цвет.G, цвет.B)),
                BorderBrush = new SolidColorBrush(цвет),
                BorderThickness = new Thickness(2),
                Child = стек
            });
        }

        ПанельЛент.Visibility = Visibility.Collapsed;
        ПанельРезультата.Visibility = Visibility.Visible;

        КнопкаОткрыть.IsEnabled = true;
        КнопкаОткрыть.Content = "🎲 ОТКРЫТЬ ЕЩЁ";
    }

    private void Закрыть_Click(object sender, RoutedEventArgs e) => Close();
}