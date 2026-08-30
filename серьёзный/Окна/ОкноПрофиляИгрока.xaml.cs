using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using серьёзный.Core.CoreProfiles;
using серьёзный.Сервисы;


namespace серьёзный.Окна;

public partial class ОкноПрофиляИгрока : Window
{
    private readonly Guid playerId;

    private readonly СервисАккаунтов accounts = new();
    private readonly ProfileStyleService styles = new();

    private readonly AchievementService achievements = new();

    public ОкноПрофиляИгрока(Guid id)
    {
        InitializeComponent();

        playerId = id;

        Loaded += (_, _) => Загрузить();
    }

    private void Загрузить()
    {
        var account = accounts.Найти(playerId.ToString());

        if (account == null)
        {
            Close();
            return;
        }

        Имя.Text = account.ПолноеИмя;
        Баланс.Text = account.ОсталосьВремени.ToString(@"hh\\:mm");

        ОбновитьПрофиль();
        ПостроитьИнвентарь();
        ПостроитьДостижения();
    }

    private void ПостроитьДостижения()
    {
        AchievementsPanel.Children.Clear();

        foreach (var item in achievements.ForProfile(playerId))
        {
            var icon = item.Unlocked ? "✔" : "🔒";

            var color =
                item.Unlocked
                    ? Brushes.LimeGreen
                    : Brushes.Gray;

            var card =
                new Border
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(14),
                    CornerRadius = new CornerRadius(12),
                    Background =
                        new SolidColorBrush(Color.FromRgb(30, 41, 59))
                };

            var stack = new StackPanel();

            stack.Children.Add(
                new TextBlock
                {
                    Text = $"{icon} {item.Info.Name}",
                    Foreground = color,
                    FontWeight = FontWeights.Bold,
                    FontSize = 16
                });

            stack.Children.Add(
                new TextBlock
                {
                    Text = item.Info.Description,
                    Foreground = Brushes.LightGray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });

            if (item.Info.RewardFrame.HasValue)
            {
                stack.Children.Add(
                    new TextBlock
                    {
                        Text =
                            $"Награда: {item.Info.RewardFrame.Value}",
                        Foreground = Brushes.Gold,
                        Margin = new Thickness(0, 6, 0, 0)
                    });
            }

            card.Child = stack;

            AchievementsPanel.Children.Add(card);
        }
    }

    private void ОбновитьПрофиль()
    {
        var style = styles.Get(playerId);

        НазваниеРамки.Text = style.Frame switch
        {
            ProfileFrame.Default => "Стандартная рамка",
            ProfileFrame.Silver => "Серебряная рамка",
            ProfileFrame.Gold => "Золотая рамка",
            ProfileFrame.Neon => "Неоновая рамка",
            ProfileFrame.Legend => "Легендарная рамка",
            _ => "Стандартная рамка"
        };

        ПрименитьРамку(style.Frame);
    }

    private void ПостроитьИнвентарь()
    {
        FramesPanel.Children.Clear();

        var owned = styles.Owned(playerId).ToHashSet();

        foreach (ProfileFrame frame in Enum.GetValues(typeof(ProfileFrame)))
        {
            bool есть = owned.Contains(frame);

            var btn = new Button
            {
                Width = 130,
                Height = 48,
                Margin = new Thickness(6),
                Content = есть
                    ? Название(frame)
                    : $"🔒 {Название(frame)}",
                IsEnabled = есть
            };

            btn.BorderThickness = new Thickness(3);
            btn.BorderBrush = Кисть(frame);

            if (!есть)
            {
                btn.Opacity = 0.45;
            }

            btn.Click += (_, _) =>
            {
                styles.SetFrame(playerId, frame);

                ОбновитьПрофиль();
            };

            FramesPanel.Children.Add(btn);
        }
    }

    private static string Название(ProfileFrame frame)
    {
        return frame switch
        {
            ProfileFrame.Default => "Стандарт",
            ProfileFrame.Silver => "Серебро",
            ProfileFrame.Gold => "Золото",
            ProfileFrame.Neon => "Неон",
            ProfileFrame.Legend => "Легенда",
            _ => frame.ToString()
        };
    }

    private static Brush Кисть(ProfileFrame frame)
    {
        return frame switch
        {
            ProfileFrame.Silver =>
                new SolidColorBrush(Color.FromRgb(210, 210, 210)),

            ProfileFrame.Gold =>
                new SolidColorBrush(Color.FromRgb(255, 204, 0)),

            ProfileFrame.Neon =>
                new SolidColorBrush(Color.FromRgb(0, 255, 255)),

            ProfileFrame.Legend =>
                new SolidColorBrush(Color.FromRgb(180, 90, 255)),

            _ =>
                new SolidColorBrush(Color.FromRgb(100, 116, 139))
        };
    }

    private void ПрименитьРамку(ProfileFrame frame)
    {
        AvatarBorder.BorderThickness = new Thickness(5);
        AvatarBorder.BorderBrush = Кисть(frame);

        AvatarBorder.Effect = null;
        AvatarBorder.RenderTransform = null;

        switch (frame)
        {
            case ProfileFrame.Gold:
                GoldAnimation();
                break;

            case ProfileFrame.Neon:
                NeonAnimation();
                break;

            case ProfileFrame.Legend:
                LegendAnimation();
                break;
        }
    }

    private void GoldAnimation()
    {
        var brush = (SolidColorBrush)AvatarBorder.BorderBrush;

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            new ColorAnimation
            {
                From = Color.FromRgb(217, 119, 6),
                To = Color.FromRgb(255, 235, 59),
                Duration = TimeSpan.FromSeconds(1.4),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            });
    }

    private void NeonAnimation()
    {
        var brush = (SolidColorBrush)AvatarBorder.BorderBrush;

        AvatarBorder.Effect =
            new DropShadowEffect
            {
                Color = Colors.Cyan,
                BlurRadius = 24,
                ShadowDepth = 0,
                Opacity = 1
            };

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            new ColorAnimation
            {
                From = Color.FromRgb(0, 255, 180),
                To = Color.FromRgb(0, 120, 255),
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            });
    }

    private void LegendAnimation()
    {
        var brush = (SolidColorBrush)AvatarBorder.BorderBrush;

        AvatarBorder.Effect =
            new DropShadowEffect
            {
                Color = Colors.MediumPurple,
                BlurRadius = 28,
                ShadowDepth = 0,
                Opacity = 1
            };

        var rotate = new RotateTransform();

        AvatarBorder.RenderTransform = rotate;
        AvatarBorder.RenderTransformOrigin = new Point(0.5, 0.5);

        rotate.BeginAnimation(
            RotateTransform.AngleProperty,
            new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(6),
                RepeatBehavior = RepeatBehavior.Forever
            });

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            new ColorAnimation
            {
                From = Color.FromRgb(180, 90, 255),
                To = Color.FromRgb(255, 0, 170),
                Duration = TimeSpan.FromSeconds(1.2),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            });
    }
}