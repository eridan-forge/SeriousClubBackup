using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace серьёзный.ЭкранКлуба.Развлечения;

public partial class ОкноСлотов : Window
{
    private readonly Random random = new();
    private readonly ЛентаСлота[] барабаны = new ЛентаСлота[3];

    private bool крутится;
    private int остановленоБарабанов;

    public ОкноСлотов()
    {
        InitializeComponent();

        for (int i = 0; i < 3; i++)
        {
            барабаны[i] = new ЛентаСлота { Margin = new Thickness(6) };
            ПанельБарабанов.Children.Add(барабаны[i]);
        }

        ТекстБаланс.Text = ТестовыйБалансРазвлечений.Баллы.ToString();
    }

    private void Дёрнуть_Click(object sender, RoutedEventArgs e)
    {
        if (крутится) return;

        if (!long.TryParse(ПолеСтавки.Text, out var ставка) || ставка <= 0)
        {
            MessageBox.Show("Введите корректную ставку.");
            return;
        }

        if (ставка > ТестовыйБалансРазвлечений.Баллы) { MessageBox.Show("Недостаточно баллов."); return; }

        крутится = true;
        остановленоБарабанов = 0;
        КнопкаДёрнуть.IsEnabled = false;
        ТекстРезультатСлотов.Text = "";

        ТестовыйБалансРазвлечений.Баллы -= ставка;
        ТекстБаланс.Text = ТестовыйБалансРазвлечений.Баллы.ToString();

        АнимироватьРычаг();

        var символы = СимволыСлотов.Получить();
        var результаты = new СимволСлота[3];

        bool большаяЛиния = random.NextDouble() < 0.12;

        if (большаяЛиния)
        {
            var общий = ВыбратьВзвешенно(символы, random);
            результаты[0] = результаты[1] = результаты[2] = общий;
        }
        else
        {
            for (int i = 0; i < 3; i++)
                результаты[i] = ВыбратьВзвешенно(символы, random);

            if (результаты[0].Иконка == результаты[1].Иконка && результаты[1].Иконка == результаты[2].Иконка)
                результаты[2] = символы.First(x => x.Иконка != результаты[0].Иконка);
        }

        for (int i = 0; i < 3; i++)
        {
            var индекс = i;
            var задержка = TimeSpan.FromMilliseconds(300 * i);
            var длительность = TimeSpan.FromSeconds(2.2 + 0.6 * i);

            var t = new DispatcherTimer { Interval = задержка };
            t.Tick += (_, _) =>
            {
                t.Stop();

                void ОбработчикОстановки(СимволСлота _)
                {
                    барабаны[индекс].Остановилась -= ОбработчикОстановки;
                    остановленоБарабанов++;

                    if (остановленоБарабанов == 3)
                        ЗавершитьВращение(ставка, результаты);
                }

                барабаны[индекс].Остановилась += ОбработчикОстановки;
                барабаны[индекс].Запустить(результаты[индекс], random, длительность);
            };
            t.Start();
        }
    }

    private static СимволСлота ВыбратьВзвешенно(List<СимволСлота> символы, Random random)
    {
        int сумма = символы.Sum(x => x.Вес);
        int бросок = random.Next(0, сумма);
        int накоплено = 0;

        foreach (var символ in символы)
        {
            накоплено += символ.Вес;
            if (бросок < накоплено)
                return символ;
        }

        return символы[^1];
    }

    private void АнимироватьРычаг()
    {
        var вниз = new DoubleAnimation(0, 130, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
        var вверх = new DoubleAnimation(130, 0, TimeSpan.FromMilliseconds(260))
        {
            BeginTime = TimeSpan.FromMilliseconds(180),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        var раскадровка = new Storyboard();
        Storyboard.SetTarget(вниз, ШарикРычага);
        Storyboard.SetTargetProperty(вниз, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
        Storyboard.SetTarget(вверх, ШарикРычага);
        Storyboard.SetTargetProperty(вверх, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

        раскадровка.Children.Add(вниз);
        раскадровка.Children.Add(вверх);
        раскадровка.Begin();
    }

    private void ЗавершитьВращение(long ставка, СимволСлота[] результаты)
    {
        long выплата = 0;

        if (результаты[0].Иконка == результаты[1].Иконка && результаты[1].Иконка == результаты[2].Иконка)
        {
            выплата = ставка * результаты[0].Выплата;
            ТекстРезультатСлотов.Text = $"🎉 ТРИ В РЯД! +{выплата}";
        }
        else if (результаты[0].Иконка == результаты[1].Иконка || результаты[1].Иконка == результаты[2].Иконка)
        {
            выплата = (long)(ставка * 0.5);
            ТекстРезультатСлотов.Text = $"Пара — +{выплата}";
        }
        else
        {
            ТекстРезультатСлотов.Text = $"😔 −{ставка}";
        }

        if (выплата > 0)
            ТестовыйБалансРазвлечений.Баллы += выплата;

        ТекстБаланс.Text = ТестовыйБалансРазвлечений.Баллы.ToString();

        крутится = false;
        КнопкаДёрнуть.IsEnabled = true;
    }
}