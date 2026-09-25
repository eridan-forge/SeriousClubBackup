using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace серьёзный.CrystalUI.Effects
{
    public static class AcrylicHelper
    {
        public static void Apply(Border border)
        {
            border.Background =
                new SolidColorBrush(
                    Color.FromArgb(180, 24, 34, 53));

            border.BorderBrush =
                new SolidColorBrush(
                    Color.FromArgb(120, 255, 255, 255));

            border.BorderThickness =
                new Thickness(1);

            border.CornerRadius =
                new CornerRadius(20);
        }
    }
}