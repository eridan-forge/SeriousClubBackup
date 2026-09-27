using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;

namespace серьёзный.ЭкранКлуба;

public class ОкноПрофиляДругогоИгрока : Window
{
    private readonly Guid целевойId;

    private readonly TextBlock имя = new() { FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
    private readonly TextBlock уровень = new() { Foreground = Brushes.LightGray, Margin = new Thickness(0, 6, 0, 0) };
    private readonly TextBlock статистика = new() { Foreground = Brushes.LightGray, Margin = new Thickness(0, 6, 0, 0) };
    private readonly TextBlock любимаяИгра = new() { Foreground = Brushes.LightGray, Margin = new Thickness(0, 6, 0, 0) };
    private readonly StackPanel трофеи = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
    private readonly TextBlock загрузка = new() { Text = "Загрузка...", Foreground = Brushes.Gray, Margin = new Thickness(0, 14, 0, 0) };

    public ОкноПрофиляДругогоИгрока(Guid targetId)
    {
        целевойId = targetId;

        Title = "Профиль игрока";
        Width = 420;
        Height = 420;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));

        var корень = new StackPanel { Margin = new Thickness(24) };

        корень.Children.Add(имя);
        корень.Children.Add(уровень);
        корень.Children.Add(статистика);
        корень.Children.Add(любимаяИгра);
        корень.Children.Add(трофеи);
        корень.Children.Add(загрузка);

        Content = корень;

        Loaded += (_, _) => _ = ЗагрузитьAsync();
    }

    private async Task ЗагрузитьAsync()
    {
        var requestId = PlayerProfileBridgeService.CreateRequest(целевойId);

        PlayerProfileDto? результат = null;

        for (int i = 0; i < 25; i++)
        {
            await Task.Delay(250);
            результат = PlayerProfileBridgeService.GetResult(requestId);
            if (результат != null) break;
        }

        загрузка.Visibility = Visibility.Collapsed;

        if (результат == null)
        {
            имя.Text = "Не удалось загрузить профиль.";
            return;
        }

        имя.Text = результат.FullName;
        уровень.Text = $"{результат.LevelName} • x{результат.LevelMultiplierPercent / 100.0:0.00}";
        статистика.Text = $"Сыграно: {TimeSpan.FromSeconds(результат.PlayedSeconds):hh\\:mm} • Сеансов: {результат.SessionCount}";

        любимаяИгра.Text = string.IsNullOrWhiteSpace(результат.FavoriteGame)
            ? "Любимая игра пока не выбрана."
            : $"Любимая игра: {результат.FavoriteGame}";

        var разблокированные = результат.Achievements.Where(x => x.Unlocked).Take(6).ToList();

        if (разблокированные.Count == 0)
        {
            трофеи.Children.Add(new TextBlock { Text = "Трофеев пока нет.", Foreground = Brushes.Gray });
        }
        else
        {
            foreach (var достижение in разблокированные)
            {
                трофеи.Children.Add(new TextBlock
                {
                    Text = "🏆",
                    FontSize = 28,
                    Margin = new Thickness(0, 0, 8, 0),
                    ToolTip = достижение.Name
                });
            }
        }
    }
}