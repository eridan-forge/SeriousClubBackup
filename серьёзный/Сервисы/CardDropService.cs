using System.Windows;
using System.Windows.Controls;

namespace серьёзный.Сервисы
{
    public static class CardDropService
    {
        public static void Enable(
            WrapPanel panel)
        {
            panel.AllowDrop = true;

            panel.Drop += (_, e) =>
            {
                if (!e.Data.GetDataPresent(typeof(Border)))
                    return;

                var card =
                    (Border)e.Data.GetData(typeof(Border));

                if (card == null)
                    return;

                panel.Children.Remove(card);

                panel.Children.Add(card);
            };
        }
    }
}