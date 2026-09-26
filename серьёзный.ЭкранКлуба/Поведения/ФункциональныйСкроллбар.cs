using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace серьёзный.ЭкранКлуба.Поведения;

public static class ФункциональныйСкроллбар
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(ФункциональныйСкроллбар),
            new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject элемент, bool значение) =>
        элемент.SetValue(EnabledProperty, значение);

    public static bool GetEnabled(DependencyObject элемент) =>
        (bool)элемент.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollBar полоса)
            return;

        полоса.Scroll -= Полоса_Scroll;

        if ((bool)e.NewValue)
            полоса.Scroll += Полоса_Scroll;
    }

    private static void Полоса_Scroll(object sender, ScrollEventArgs e)
    {
        if (sender is not ScrollBar полоса)
            return;

        НайтиРодителя<ScrollViewer>(полоса)?.ScrollToVerticalOffset(e.NewValue);
    }

    private static T? НайтиРодителя<T>(DependencyObject узел) where T : DependencyObject
    {
        while (узел != null)
        {
            if (узел is T найден)
                return найден;

            узел = VisualTreeHelper.GetParent(узел);
        }

        return null;
    }
}