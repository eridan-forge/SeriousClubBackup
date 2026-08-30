
using System;
using System.Windows;

namespace серьёзный.Окна
{
    public partial class ОкноОстатка002 : Window
    {
        public bool ИспользоватьОстаток { get; private set; }

        public ОкноОстатка002(
            string имя,
            TimeSpan остаток)
        {
            InitializeComponent();

            Имя.Text = имя;

            Остаток.Text =
                остаток.ToString(@"hh\:mm");
        }

        private void Использовать(
            object sender,
            RoutedEventArgs e)
        {
            ИспользоватьОстаток = true;

            DialogResult = true;
        }

        private void Новое(
            object sender,
            RoutedEventArgs e)
        {
            ИспользоватьОстаток = false;

            DialogResult = true;
        }

        private void Отмена(
            object sender,
            RoutedEventArgs e)
        {
            ИспользоватьОстаток = false;

            DialogResult = false;
        }
    }
}

