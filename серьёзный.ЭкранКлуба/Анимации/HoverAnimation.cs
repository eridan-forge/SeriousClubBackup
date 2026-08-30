using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace серьёзный.ЭкранКлуба.Анимации;

public static class HoverAnimation
{
    public static void Attach(FrameworkElement element)
    {
        var scale = new ScaleTransform(1, 1);

        element.RenderTransformOrigin =
            new Point(.5, .5);

        element.RenderTransform = scale;

        element.MouseEnter += (_, _) =>
        {
            Animate(scale, 1.05);
        };

        element.MouseLeave += (_, _) =>
        {
            Animate(scale, 1);
        };
    }

    private static void Animate(
        ScaleTransform scale,
        double value)
    {
        var anim = new DoubleAnimation
        {
            To = value,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new QuadraticEase()
        };

        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            anim);

        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            anim);
    }
}