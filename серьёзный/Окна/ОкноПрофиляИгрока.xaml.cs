using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using серьёзный.Core.CoreEconomy;
using серьёзный.Core.CoreProfiles;
using серьёзный.Core.CoreShop;
using серьёзный.Модели;
using серьёзный.Сервисы;


namespace серьёзный.Окна;

public partial class ОкноПрофиляИгрока : Window
{
    private readonly Guid playerId;

    private readonly СервисАккаунтов accounts = new();
    private readonly ProfileStyleService styles = new();
    private readonly AchievementService achievements = new();

    private readonly PointsService points = new();
    private readonly LevelService levels = new();
    private readonly СервисАрхива005 архив = new();
    private readonly ShopRequestService заказы = new();

    private readonly DispatcherTimer таймер = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };

    public ОкноПрофиляИгрока(Guid id)
    {
        InitializeComponent();

        playerId = id;

        Loaded += (_, _) =>
        {
            Загрузить();

            таймер.Tick += (_, _) => Загрузить();
            таймер.Start();
        };

        Closed += (_, _) => таймер.Stop();
    }

    private void Загрузить()
    {
        var account = accounts.Получить(playerId);

        if (account == null)
        {
            таймер.Stop();
            Close();
            return;
        }

        Имя.Text = account.ПолноеИмя;
        Баланс.Text = account.ОсталосьВремени.ToString(@"hh\:mm");

        ОбновитьЭкономику(account);
        ОбновитьПрофиль();
        ПостроитьИнвентарь();
        ПостроитьДостижения();
        ПостроитьИсторию();
    }

    private void ОбновитьЭкономику(АккаунтИгрока account)
    {
        var баланс = points.Get(playerId);

        ТекстБаллы.Text = $"⭐ {баланс.Points}";

        var уровень =
            levels.GetTierByPlayedSeconds(
                (long)account.ВсегоСыграно.TotalSeconds);

        ТекстУровень.Text =
            $"{уровень.Name} (x{уровень.MultiplierPercent / 100.0:0.00})";

        var активенПремиум =
            баланс.Premium &&
            (!баланс.PremiumUntil.HasValue ||
             баланс.PremiumUntil.Value > DateTime.Now);

        ТекстПремиум.Text =
            !активенПремиум
                ? "Нет"
                : баланс.PremiumUntil.HasValue
                    ? $"⭐ до {баланс.PremiumUntil.Value:dd.MM.yyyy}"
                    : "⭐ Бессрочно";
    }

    private void ПостроитьИсторию()
    {
        SessionsHistoryPanel.Children.Clear();

        var сеансы =
            архив.ПолучитьВсе()
                 .Where(x => x.АккаунтGuid == playerId)
                 .OrderByDescending(x => x.Начало)
                 .Take(5)
                 .ToList();

        if (сеансы.Count == 0)
        {
            SessionsHistoryPanel.Children.Add(
                СтрокаИстории("Пока нет завершённых сеансов.", ""));
        }

        foreach (var сеанс in сеансы)
        {
            var сыграно = сеанс.Сыграно.ToString(@"hh\:mm");

            SessionsHistoryPanel.Children.Add(
                СтрокаИстории(
                    $"ПК-{сеанс.КомпьютерId} • {сеанс.Начало:dd.MM HH:mm}",
                    сыграно));
        }

        PurchasesHistoryPanel.Children.Clear();

        var покупки =
            заказы.All
                  .Where(x => x.AccountId == playerId)
                  .OrderByDescending(x => x.Time)
                  .Take(5)
                  .ToList();

        if (покупки.Count == 0)
        {
            PurchasesHistoryPanel.Children.Add(
                СтрокаИстории("Покупок пока нет.", ""));
        }

        foreach (var покупка in покупки)
        {
            PurchasesHistoryPanel.Children.Add(
                СтрокаИстории(
                    $"{покупка.ItemName} • {покупка.Time:dd.MM HH:mm}",
                    $"{покупка.Price:0} ₽"));
        }
    }

    private static UIElement СтрокаИстории(string текст, string значение)
    {
        var row = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };

        row.Children.Add(new TextBlock
        {
            Text = текст,
            Foreground = Brushes.LightGray,
            FontSize = 13
        });

        var значениеБлок = new TextBlock
        {
            Text = значение,
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.Bold
        };

        DockPanel.SetDock(значениеБлок, Dock.Right);

        row.Children.Add(значениеБлок);

        return row;
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
            null);

        var анимация =
            new ColorAnimation
            {
                From = Color.FromRgb(255, 204, 0),
                To = Color.FromRgb(255, 240, 150),
                Duration = TimeSpan.FromSeconds(1.2),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            анимация);

        AvatarBorder.Effect =
            new DropShadowEffect
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

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            null);

        var анимация =
            new ColorAnimation
            {
                From = Color.FromRgb(0, 255, 255),
                To = Color.FromRgb(0, 140, 255),
                Duration = TimeSpan.FromSeconds(0.9),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            анимация);

        AvatarBorder.Effect =
            new DropShadowEffect
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

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            null);

        var анимация =
            new ColorAnimation
            {
                From = Color.FromRgb(180, 90, 255),
                To = Color.FromRgb(255, 90, 200),
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            анимация);

        var scale = new ScaleTransform(1, 1);

        AvatarBorder.RenderTransformOrigin = new Point(0.5, 0.5);
        AvatarBorder.RenderTransform = scale;

        var pulse =
            new DoubleAnimation
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

        AvatarBorder.Effect =
            new DropShadowEffect
            {
                Color = Color.FromRgb(180, 90, 255),
                BlurRadius = 36,
                ShadowDepth = 0,
                Opacity = 0.9
            };
    }
}