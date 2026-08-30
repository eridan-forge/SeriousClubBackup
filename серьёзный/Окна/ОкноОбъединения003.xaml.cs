using System;
using System.Windows;

namespace серьёзный.Окна
{
    public partial class ОкноОбъединения003 : Window
    {
        public bool ДобавитьОстаток { get; private set; }

        public ОкноОбъединения003(
            TimeSpan покупка,
            TimeSpan остаток)
        {
            InitializeComponent();

            Покупка.Text =
                ФорматВремени(покупка);

            Остаток.Text =
                ФорматВремени(остаток);
        }

        private static string ФорматВремени(
            TimeSpan время)
        {
            if (время.TotalDays >= 1)
            {
                return $"{(int)время.TotalDays}д {время.Hours:00}:{время.Minutes:00}";
            }

            return время.ToString(@"hh\:mm");
        }

        private void Да_Click(
            object sender,
            RoutedEventArgs e)
        {
            ДобавитьОстаток = true;
            DialogResult = true;
        }

        private void Нет_Click(
            object sender,
            RoutedEventArgs e)
        {
            ДобавитьОстаток = false;
            DialogResult = true;
        }

        private void Отмена_Click(
            object sender,
            RoutedEventArgs e)
        {
            ДобавитьОстаток = false;
            DialogResult = false;
        }
    }
}