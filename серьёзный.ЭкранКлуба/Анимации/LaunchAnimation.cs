using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace серьёзный.ЭкранКлуба.Анимации;

public static class LaunchAnimation
{
    public static void Play(UIElement card)
    {
        var scale = new ScaleTransform(1, 1);

        card.RenderTransformOrigin = new Point(.5, .5);

        card.RenderTransform = scale;

        var anim = new DoubleAnimation
        {
            From = 1,
            To = 1.08,
            Duration = TimeSpan.FromMilliseconds(180),
            AutoReverse = true
        };

        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            anim);

        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            anim);
    }
}