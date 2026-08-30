using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace серьёзный.ЭкранКлуба.Анимации;

public static class ShopTickerAnimation
{
    public static void Start(StackPanel panel)
    {
        var animation =
            new DoubleAnimation
            {
                From = 0,
                To = -900,
                Duration =
                    TimeSpan.FromSeconds(22),
                RepeatBehavior =
                    RepeatBehavior.Forever
            };

        panel.BeginAnimation(
            Canvas.LeftProperty,
            animation);
    }
}