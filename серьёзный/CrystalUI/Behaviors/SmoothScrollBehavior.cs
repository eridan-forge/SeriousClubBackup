using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace серьёзный.CrystalUI.Behaviors;

// ScrollViewer.VerticalOffset — read-only DP, напрямую анимировать
// нельзя. Используем медиатор: обычное animatable-свойство, чьё
// изменение на каждом кадре дёргает ScrollToVerticalOffset.
public static class SmoothScrollBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(SmoothScrollBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject element, bool value) =>
        element.SetValue(EnabledProperty, value);

    public static bool GetEnabled(DependencyObject element) =>
        (bool)element.GetValue(EnabledProperty);

    private static readonly DependencyProperty МедиаторOffsetProperty =
        DependencyProperty.RegisterAttached(
            "МедиаторOffset", typeof(double), typeof(SmoothScrollBehavior),
            new PropertyMetadata(0.0, OnМедиаторOffsetChanged));

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer viewer)
            return;

        if ((bool)e.NewValue)
            viewer.PreviewMouseWheel += Viewer_PreviewMouseWheel;
        else
            viewer.PreviewMouseWheel -= Viewer_PreviewMouseWheel;
    }

    private static void Viewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer)
            return;

        e.Handled = true;

        var текущее = (double)viewer.GetValue(МедиаторOffsetProperty);

        if (Math.Abs(текущее - viewer.VerticalOffset) > 2)
            текущее = viewer.VerticalOffset;

        var цель = Math.Max(0, Math.Min(viewer.ScrollableHeight, текущее - Math.Sign(e.Delta) * 110));

        var анимация = new DoubleAnimation(текущее, цель, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        viewer.BeginAnimation(МедиаторOffsetProperty, анимация);
    }

    private static void OnМедиаторOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer viewer)
            viewer.ScrollToVerticalOffset((double)e.NewValue);
    }
}