using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace серьёзный.CrystalUI.Animations
{
    public static class HoverAnimation
    {
        public static void Attach(Border card)
        {
            var scale = new ScaleTransform(1, 1);
            card.RenderTransform = scale;
            card.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            card.MouseEnter += (_, __) =>
            {
                Animate(scale, 1.05);
            };

            card.MouseLeave += (_, __) =>
            {
                Animate(scale, 1);
            };
        }

        private static void Animate(
            ScaleTransform transform,
            double value)
        {
            var anim = new DoubleAnimation
            {
                To = value,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction =
                    new CubicEase
                    {
                        EasingMode = EasingMode.EaseOut
                    }
            };

            transform.BeginAnimation(
                ScaleTransform.ScaleXProperty,
                anim);

            transform.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                anim);
        }
    }
}