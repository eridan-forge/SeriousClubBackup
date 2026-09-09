using System;
using System.Windows;
using System.Windows.Input;

namespace серьёзный.Окна;

public partial class QuickAccessBar : Window
{
    public event Action? ВосстановитьЗапрошено;

    private Point точкаНачала;
    private bool перетаскивается;

    private const double ПорогПеретаскивания = 4;

    public QuickAccessBar()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            Left = SystemParameters.WorkArea.Right - Width - 24;
            Top = SystemParameters.WorkArea.Bottom - Height - 24;
        };
    }

    private void Панель_MouseDown(object sender, MouseButtonEventArgs e)
    {
        точкаНачала = e.GetPosition(this);
        перетаскивается = false;

        ((UIElement)sender).CaptureMouse();
    }

    private void Панель_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var текущая = e.GetPosition(this);

        var сдвигX = текущая.X - точкаНачала.X;
        var сдвигY = текущая.Y - точкаНачала.Y;

        if (!перетаскивается &&
            (Math.Abs(сдвигX) > ПорогПеретаскивания ||
             Math.Abs(сдвигY) > ПорогПеретаскивания))
        {
            перетаскивается = true;
        }

        if (перетаскивается)
        {
            Left += сдвигX;
            Top += сдвигY;
        }
    }

    private void Панель_MouseUp(object sender, MouseButtonEventArgs e)
    {
        ((UIElement)sender).ReleaseMouseCapture();

        if (!перетаскивается)
            ВосстановитьЗапрошено?.Invoke();

        перетаскивается = false;
    }
}