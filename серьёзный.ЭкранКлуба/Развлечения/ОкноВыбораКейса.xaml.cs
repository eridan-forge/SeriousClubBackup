using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace серьёзный.ЭкранКлуба.Развлечения;

public partial class ОкноВыбораКейса : Window
{
    public ОкноВыбораКейса()
    {
        InitializeComponent();

        foreach (var кейс in ТестовыеКейсы.Получить())
            Панель.Children.Add(СоздатьКарточку(кейс));
    }

    private Border СоздатьКарточку(ОпределениеКейса кейс)
    {
        var стек = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

        стек.Children.Add(new TextBlock { Text = кейс.Иконка, FontSize = 46, HorizontalAlignment = HorizontalAlignment.Center });
        стек.Children.Add(new TextBlock
        {
            Text = кейс.Название,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 15,
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var карточка = new Border
        {
            Width = 190,
            Height = 190,
            Margin = new Thickness(10),
            CornerRadius = new CornerRadius(16),
            Background = (Brush)FindResource("ФонКарточкиАльт"),
            BorderBrush = (Brush)FindResource("РамкаЦвет"),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = стек
        };

        карточка.MouseLeftButtonUp += (_, _) =>
        {
            new ОкноОткрытияКейса(кейс) { Owner = this }.ShowDialog();
        };

        return карточка;
    }
}