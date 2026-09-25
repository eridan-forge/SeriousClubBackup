using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace серьёзный.Сервисы;

public static class CardReorderService
{
    public static void Enable(WrapPanel panel)
    {
        UIElement? drag = null;

        panel.PreviewMouseLeftButtonDown += (_, e) =>
        {
            drag = e.OriginalSource as UIElement;
        };

        panel.MouseMove += (_, e) =>
        {
            if (drag == null)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            DragDrop.DoDragDrop(
                drag,
                drag,
                DragDropEffects.Move);
        };

        panel.Drop += (_, e) =>
        {
            if (drag == null)
                return;

            var target =
                e.OriginalSource as UIElement;

            if (target == null)
                return;

            int oldIndex =
                panel.Children.IndexOf(drag);

            int newIndex =
                panel.Children.IndexOf(target);

            if (oldIndex < 0 || newIndex < 0)
                return;

            panel.Children.Remove(drag);
            panel.Children.Insert(newIndex, drag);

            drag = null;
        };
    }
}