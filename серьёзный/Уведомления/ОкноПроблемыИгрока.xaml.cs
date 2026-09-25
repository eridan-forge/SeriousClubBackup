using System.Windows;

namespace серьёзный.Уведомления;

public partial class ОкноПроблемыИгрока : Window
{
    public ОкноПроблемыИгрока(int pcId, string имяИгрока, string текст)
    {
        InitializeComponent();

        Текст.Text = $"ПК-{pcId:D2} • {имяИгрока}\n{текст}";

        Loaded += (_, _) =>
        {
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;
        };

        Готово.Click += (_, _) => Close();
        Закрыть.Click += (_, _) => Close();
    }
}