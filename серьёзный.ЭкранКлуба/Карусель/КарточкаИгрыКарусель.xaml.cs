using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using серьёзный.Модели;

namespace серьёзный.ЭкранКлуба.Карусель;

public partial class КарточкаИгрыКарусель : UserControl
{
    // Внешний "угол поворота", который задаёт КарусельИгр (те же условные
    // единицы -62..62, что раньше уходили в PlaneProjection.RotationY),
    // пересчитывается в угол SkewTransform - те же числа "в лоб" у Skew
    // выглядели бы намного резче настоящего поворота.
    private const double КоэффициентНаклона = 0.34;

    public Игра? Игра { get; private set; }

    public event Action<Игра>? ИграЗапущена;
    public event Action<Игра>? ИзбранноеИзменилось;
    public event Action<Игра>? ИграСкрыта;

    public КарточкаИгрыКарусель()
    {
        InitializeComponent();
    }

    public void Загрузить(Игра игра, bool избранное)
    {
        Игра = игра;

        Название.Text = игра.Название;
        Категория.Text = игра.Категория;

        КнопкаИзбранное.Content = избранное ? "★" : "☆";

        КнопкаИзбранное.Foreground = избранное
            ? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)) // золото
            : Brushes.White;

        if (!string.IsNullOrWhiteSpace(игра.Обложка) && File.Exists(игра.Обложка))
        {
            var картинка = new BitmapImage();

            картинка.BeginInit();
            картинка.CacheOption = BitmapCacheOption.OnLoad;
            картинка.UriSource = new Uri(игра.Обложка);
            картинка.EndInit();
            картинка.Freeze();

            Обложка.Source = картинка;
            ОбложкаРазмытая.Source = картинка;

            Обложка.Visibility = Visibility.Visible;
            ОбложкаРазмытая.Visibility = Visibility.Visible;
            ЗаглушкаОбложки.Visibility = Visibility.Collapsed;
        }
        else
        {
            Обложка.Source = null;
            ОбложкаРазмытая.Source = null;

            Обложка.Visibility = Visibility.Collapsed;
            ОбложкаРазмытая.Visibility = Visibility.Collapsed;
            ЗаглушкаОбложки.Visibility = Visibility.Visible;
        }
    }

    // =====================================================
    // ГЛУБИНА / ПОЗИЦИЯ В КАРУСЕЛИ — управляется КарусельюИгр,
    // сама карточка ничего не решает про свою позицию в дуге.
    // =====================================================

    public double МасштабКарточки
    {
        get => Масштаб.ScaleX;
        set
        {
            Масштаб.ScaleX = value;
            Масштаб.ScaleY = value;
        }
    }

    // добавить в класс КарточкаИгрыКарусель
    public double РадиусСкругления
    {
        get => ГраницаОбрезки.RadiusX;
        set
        {
            ГраницаОбрезки.RadiusX = value;
            ГраницаОбрезки.RadiusY = value;
        }
    }

    // Имя и назначение свойства не изменились (КарусельИгр.xaml.cs
    // использует его как раньше, без правок) - изменилась только
    // начинка: вместо PlaneProjection.RotationY крутит SkewTransform.
    public double УголПоворота
    {
        get => Наклон.AngleY / КоэффициентНаклона;
        set => Наклон.AngleY = value * КоэффициентНаклона;
    }

    private void КнопкаИграть_Click(object sender, RoutedEventArgs e)
    {
        if (Игра != null)
            ИграЗапущена?.Invoke(Игра);
    }

    private void КнопкаИзбранное_Click(object sender, RoutedEventArgs e)
    {
        if (Игра != null)
            ИзбранноеИзменилось?.Invoke(Игра);
    }

    private void КнопкаСкрыть_Click(object sender, RoutedEventArgs e)
    {
        if (Игра != null)
            ИграСкрыта?.Invoke(Игра);
    }
}