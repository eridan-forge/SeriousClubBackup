using System;
using System.Windows;

namespace серьёзный.Окна
{
    public partial class ОкноПодтверждениеЗавершения : Window
    {
        public bool Подтверждено { get; private set; }

        public ОкноПодтверждениеЗавершения(
            TimeSpan осталось)
        {
            InitializeComponent();

            var отображаемое =
                осталось < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : осталось;

            Описание.Text =
                $"После вычитания останется 0 времени.\n\n" +
                $"Сейчас осталось: {отображаемое:hh\\:mm\\:ss}\n\n" +
                "Подтвердить полное завершение сеанса?";
        }

        private void Отмена_Click(
            object sender,
            RoutedEventArgs e)
        {
            Подтверждено = false;
            DialogResult = false;
            Close();
        }

        private void Завершить_Click(
            object sender,
            RoutedEventArgs e)
        {
            Подтверждено = true;
            DialogResult = true;
            Close();
        }
    }
}