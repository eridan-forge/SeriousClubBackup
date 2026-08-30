using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace серьёзный.Сервисы
{
    public static class CardDragService
    {
        public static void Enable(
            Border card)
        {
            card.MouseMove += (_, e) =>
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                    return;

                DragDrop.DoDragDrop(
                    card,
                    card,
                    DragDropEffects.Move);
            };
        }
    }
}