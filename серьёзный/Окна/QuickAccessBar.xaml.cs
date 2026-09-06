using System;
using System.Windows;
using System.Windows.Input;

namespace серьёзный.Окна;

public partial class QuickAccessBar : Window
{
    public event Action? ВосстановитьЗапрошено;

    public QuickAccessBar()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            Left = SystemParameters.WorkArea.Right - Width - 24;
            Top = SystemParameters.WorkArea.Bottom - Height - 24;
        };
    }

    private void Панель_Click(object sender, MouseButtonEventArgs e)
    {
        ВосстановитьЗапрошено?.Invoke();
    }
}