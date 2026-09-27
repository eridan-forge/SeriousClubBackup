using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace серьёзный.ЭкранКлуба.Развлечения;

public enum ЦветРулетки { Красное, Чёрное, Зелёное }

public partial class ОкноРулетки : Window
{
    private const int КоличествоСекторов = 15;
    private const double РадиусКолеса = 210;
    private const double ЦентрX = 230;
    private const double ЦентрY = 230;

    private readonly ЦветРулетки[] секторы = new ЦветРулетки[КоличествоСекторов];
    private readonly Random random = new();

    private ЦветРулетки? выбранныйЦвет;
    private bool крутится;

    public ОкноРулетки()
    {
        InitializeComponent();

        ЗаполнитьСекторы();
        НарисоватьКолесо();

        ТекстБаланс.Text = ТестовыйБалансРазвлечений.Баллы.ToString();
    }

    private void ЗаполнитьСекторы()
    {
        секторы[0] = ЦветРулетки.Зелёное;

        for (int i = 1; i < КоличествоСекторов; i++)
            секторы[i] = i % 2 == 0 ? ЦветРулетки.Красное : ЦветРулетки.Чёрное;
    }

    private void НарисоватьКолесо()
    {
        ХолстКолеса.Children.Clear();

        double уголНаСектор = 360.0 / КоличествоСекторов;

        for (int i = 0; i < КоличествоСекторов; i++)
        {
            double начало = i * уголНаСектор;
            double конец = начало + уголНаСектор;

            ХолстКолеса.Children.Add(СоздатьСектор(ЦентрX, ЦентрY, РадиусКолеса, начало, конец, КистьЦвета(секторы[i])));
        }
    }

    private static Brush КистьЦвета(ЦветРулетки цвет) => цвет switch
    {
        ЦветРулетки.Красное => new SolidColorBrush(Color.FromRgb(0xC0, 0x1F, 0x2E)),
        ЦветРулетки.Зелёное => new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D)),
        _ => new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1C))
    };

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

        return new Path { Data = geometry, Fill = заливка, Stroke = Brushes.Black, StrokeThickness = 1 };
    }

    private void ВыбратьЦвет_Click(object sender, RoutedEventArgs e)
    {
        if (sender == КнопкаКрасное) выбранныйЦвет = ЦветРулетки.Красное;
        else if (sender == КнопкаЧёрное) выбранныйЦвет = ЦветРулетки.Чёрное;
        else выбранныйЦвет = ЦветРулетки.Зелёное;

        foreach (var кнопка in new[] { КнопкаКрасное, КнопкаЗелёное, КнопкаЧёрное })
            кнопка.Tag = null;

        ((Button)sender).Tag = "Активна";
    }

    private void Крутить_Click(object sender, RoutedEventArgs e)
    {
        if (крутится) return;

        if (выбранныйЦвет == null) { MessageBox.Show("Выберите цвет ставки."); return; }

        if (!long.TryParse(ПолеСтавки.Text, out var ставка) || ставка <= 0)
        {
            MessageBox.Show("Введите корректную ставку.");
            return;
        }

        if (ставка > ТестовыйБалансРазвлечений.Баллы) { MessageBox.Show("Недостаточно баллов."); return; }

        крутится = true;
        КнопкаКрутить.IsEnabled = false;
        ТекстРезультат.Text = "";

        ТестовыйБалансРазвлечений.Баллы -= ставка;
        ТекстБаланс.Text = ТестовыйБалансРазвлечений.Баллы.ToString();

        int индексПобедителя = random.Next(0, КоличествоСекторов);
        double уголНаСектор = 360.0 / КоличествоСекторов;
        double целевойУгол = индексПобедителя * уголНаСектор + уголНаСектор / 2.0;

        double текущийУгол = ВращениеШарика.Angle % 360;
        double итоговыйУгол = ВращениеШарика.Angle + 6 * 360 + ((360 - текущийУгол) + целевойУгол);

        var анимация = new DoubleAnimation(ВращениеШарика.Angle, итоговыйУгол, TimeSpan.FromSeconds(5.5))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        анимация.Completed += (_, _) =>
        {
            ВращениеШарика.Angle %= 360;

            var выпавшийЦвет = секторы[индексПобедителя];
            bool выигрыш = выпавшийЦвет == выбранныйЦвет;
            long множитель = выпавшийЦвет == ЦветРулетки.Зелёное ? 14 : 2;

            if (выигрыш)
            {
                var выплата = ставка * множитель;
                ТестовыйБалансРазвлечений.Баллы += выплата;
                ТекстРезультат.Text = $"{НазваниеЦвета(выпавшийЦвет)}\n🎉 +{выплата}";
            }
            else
            {
                ТекстРезультат.Text = $"{НазваниеЦвета(выпавшийЦвет)}\n😔 −{ставка}";
            }

            ТекстБаланс.Text = ТестовыйБалансРазвлечений.Баллы.ToString();

            крутится = false;
            КнопкаКрутить.IsEnabled = true;
        };

        ВращениеШарика.BeginAnimation(RotateTransform.AngleProperty, анимация);
    }

    private static string НазваниеЦвета(ЦветРулетки цвет) => цвет switch
    {
        ЦветРулетки.Красное => "🔴 Красное",
        ЦветРулетки.Зелёное => "🟢 Зелёное",
        _ => "⚫ Чёрное"
    };
}