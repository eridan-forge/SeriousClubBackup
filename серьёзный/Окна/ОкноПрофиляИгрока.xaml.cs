using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;

namespace серьёзный.Окна;

public partial class ОкноПрофиляИгрока : Window
{
    private readonly Guid playerId;

    private readonly DispatcherTimer таймер = new()
    {
        Interval = TimeSpan.FromSeconds(3)
    };

    private PlayerProfileDto? профиль;

    public ОкноПрофиляИгрока(Guid id)
    {
        InitializeComponent();

        playerId = id;

        Loaded += (_, _) =>
        {
            _ = ЗагрузитьAsync();

            таймер.Tick += (_, _) => _ = ЗагрузитьAsync();
            таймер.Start();
        };

        Closed += (_, _) => таймер.Stop();
    }

    private async Task ЗагрузитьAsync()
    {
        var requestId = PlayerProfileBridgeService.CreateRequest(playerId);

        PlayerProfileDto? результат = null;

        for (int i = 0; i < 20; i++) // до ~6 сек
        {
            await Task.Delay(300);

            результат = PlayerProfileBridgeService.GetResult(requestId);

            if (результат != null)
                break;
        }

        if (результат == null)
        {
            if (профиль == null)
            {
                таймер.Stop();
                Close();
            }

            return;
        }

        профиль = результат;

        Отрисовать();
    }

    private void Отрисовать()
    {
        if (профиль == null)
            return;

        Имя.Text = профиль.FullName;
        Баланс.Text = TimeSpan.FromSeconds(профиль.RemainingSeconds).ToString(@"hh\:mm");

        ТекстБаллы.Text = $"⭐ {профиль.Points}";
        ТекстУровень.Text = $"{профиль.LevelName} (x{профиль.LevelMultiplierPercent / 100.0:0.00})";

        ТекстПремиум.Text = !профиль.Premium
            ? "Нет"
            : профиль.PremiumUntil.HasValue
                ? $"⭐ до {профиль.PremiumUntil.Value:dd.MM.yyyy}"
                : "⭐ Бессрочно";

        var текущаяРамка = (серьёзный.Core.CoreProfiles.ProfileFrame)профиль.CurrentFrame;

        НазваниеРамки.Text = Название(текущаяРамка);

        ПрименитьРамку(текущаяРамка);

        ПостроитьИнвентарь();
        ПостроитьДостижения();
    }

    private void ПостроитьДостижения()
    {
        AchievementsPanel.Children.Clear();

        foreach (var item in профиль!.Achievements)
        {
            var icon = item.Unlocked ? "✔" : "🔒";
            var color = item.Unlocked ? Brushes.LimeGreen : Brushes.Gray;

            var card = new Border
            {
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(14),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = $"{icon} {item.Name}",
                Foreground = color,
                FontWeight = FontWeights.Bold,
                FontSize = 16
            });

            stack.Children.Add(new TextBlock
            {
                Text = item.Description,
                Foreground = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });

            if (item.RewardFrame.HasValue)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Награда: {Название((серьёзный.Core.CoreProfiles.ProfileFrame)item.RewardFrame.Value)}",
                    Foreground = Brushes.Gold,
                    Margin = new Thickness(0, 6, 0, 0)
                });
            }

            card.Child = stack;

            AchievementsPanel.Children.Add(card);
        }
    }

    private void ПостроитьИнвентарь()
    {
        FramesPanel.Children.Clear();

        foreach (var frameDto in профиль!.Frames)
        {
            var frame = (серьёзный.Core.CoreProfiles.ProfileFrame)frameDto.Frame;

            var btn = new Button
            {
                Width = 130,
                Height = 48,
                Margin = new Thickness(6),
                Content = frameDto.Owned ? Название(frame) : $"🔒 {Название(frame)}",
                IsEnabled = false,
                BorderThickness = new Thickness(3),
                BorderBrush = Кисть(frame),
                Opacity = frameDto.Owned ? 1 : 0.45
            };

            FramesPanel.Children.Add(btn);
        }
    }

    private static string Название(серьёзный.Core.CoreProfiles.ProfileFrame frame)
    {
        return frame switch
        {
            серьёзный.Core.CoreProfiles.ProfileFrame.Default => "Стандарт",
            серьёзный.Core.CoreProfiles.ProfileFrame.Silver => "Серебро",
            серьёзный.Core.CoreProfiles.ProfileFrame.Gold => "Золото",
            серьёзный.Core.CoreProfiles.ProfileFrame.Neon => "Неон",
            серьёзный.Core.CoreProfiles.ProfileFrame.Legend => "Легенда",
            _ => frame.ToString()
        };
    }

    private static Brush Кисть(серьёзный.Core.CoreProfiles.ProfileFrame frame)
    {
        return frame switch
        {
            серьёзный.Core.CoreProfiles.ProfileFrame.Silver =>
                new SolidColorBrush(Color.FromRgb(210, 210, 210)),
            серьёзный.Core.CoreProfiles.ProfileFrame.Gold =>
                new SolidColorBrush(Color.FromRgb(255, 204, 0)),
            серьёзный.Core.CoreProfiles.ProfileFrame.Neon =>
                new SolidColorBrush(Color.FromRgb(0, 255, 255)),
            серьёзный.Core.CoreProfiles.ProfileFrame.Legend =>
                new SolidColorBrush(Color.FromRgb(180, 90, 255)),
            _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))
        };
    }

    private void ПрименитьРамку(серьёзный.Core.CoreProfiles.ProfileFrame frame)
    {
        AvatarBorder.BorderThickness = new Thickness(5);
        AvatarBorder.BorderBrush = Кисть(frame);
        AvatarBorder.Effect = null;
        AvatarBorder.RenderTransform = null;

        switch (frame)
        {
            case серьёзный.Core.CoreProfiles.ProfileFrame.Gold:
                GoldAnimation();
                break;
            case серьёзный.Core.CoreProfiles.ProfileFrame.Neon:
                NeonAnimation();
                break;
            case серьёзный.Core.CoreProfiles.ProfileFrame.Legend:
                LegendAnimation();
                break;
        }
    }

    private void GoldAnimation()
    {
        var brush = (SolidColorBrush)AvatarBorder.BorderBrush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, null);

        var анимация = new ColorAnimation
        {
            From = Color.FromRgb(255, 204, 0),
            To = Color.FromRgb(255, 240, 150),
            Duration = TimeSpan.FromSeconds(1.2),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        brush.BeginAnimation(SolidColorBrush.ColorProperty, анимация);

        AvatarBorder.Effect = new DropShadowEffect
        {
            Color = Color.FromRgb(255, 204, 0),
            BlurRadius = 24,
            ShadowDepth = 0,
            Opacity = 0.75
        };
    }

    private void NeonAnimation()
    {
        var brush = (SolidColorBrush)AvatarBorder.BorderBrush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, null);

        var анимация = new ColorAnimation
        {
            From = Color.FromRgb(0, 255, 255),
            To = Color.FromRgb(0, 140, 255),
            Duration = TimeSpan.FromSeconds(0.9),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        brush.BeginAnimation(SolidColorBrush.ColorProperty, анимация);

        AvatarBorder.Effect = new DropShadowEffect
        {
            Color = Color.FromRgb(0, 255, 255),
            BlurRadius = 30,
            ShadowDepth = 0,
            Opacity = 0.85
        };
    }

    private void LegendAnimation()
    {
        var brush = (SolidColorBrush)AvatarBorder.BorderBrush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, null);

        var анимация = new ColorAnimation
        {
            From = Color.FromRgb(180, 90, 255),
            To = Color.FromRgb(255, 90, 200),
            Duration = TimeSpan.FromSeconds(1.5),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        brush.BeginAnimation(SolidColorBrush.ColorProperty, анимация);

        var scale = new ScaleTransform(1, 1);

        AvatarBorder.RenderTransformOrigin = new Point(0.5, 0.5);
        AvatarBorder.RenderTransform = scale;

        var pulse = new DoubleAnimation
        {
            From = 1.0,
            To = 1.05,
            Duration = TimeSpan.FromSeconds(1.0),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);

        AvatarBorder.Effect = new DropShadowEffect
        {
            Color = Color.FromRgb(180, 90, 255),
            BlurRadius = 36,
            ShadowDepth = 0,
            Opacity = 0.9
        };
    }
}