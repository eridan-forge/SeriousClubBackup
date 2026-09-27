using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace серьёзный.ЭкранКлуба.Развлечения;

public partial class ЛентаСлота : UserControl
{
    private const double ВысотаСимвола = 110;
    private const int ВсегоСимволов = 30;
    private const int ИндексОстановки = 24;

    public event Action<СимволСлота>? Остановилась;

    public ЛентаСлота()
    {
        InitializeComponent();
    }

    public void Запустить(СимволСлота результат, Random random, TimeSpan длительность)
    {
        var символы = СимволыСлотов.Получить();

        Лента.Children.Clear();

        for (int i = 0; i < ВсегоСимволов; i++)
        {
            var символ = i == ИндексОстановки ? результат : символы[random.Next(символы.Count)];
            Лента.Children.Add(СоздатьЯчейку(символ));
        }

        Лента.Height = ВысотаСимвола * ВсегоСимволов;

        double целеваяПозиция = -(ИндексОстановки * ВысотаСимвола - (330 / 2.0 - ВысотаСимвола / 2.0));

        var анимация = new DoubleAnimation(0, целеваяПозиция, длительность)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        анимация.Completed += (_, _) => Остановилась?.Invoke(результат);

        Сдвиг.BeginAnimation(TranslateTransform.YProperty, анимация);
    }

    private Border СоздатьЯчейку(СимволСлота символ)
    {
        return new Border
        {
            Height = ВысотаСимвола,
            Width = 120,
            Child = new TextBlock
            {
                Text = символ.Иконка,
                FontSize = 52,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }
}