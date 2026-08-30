using System;
using System.Windows;
using серьёзный.Сервисы;

namespace серьёзный.Окна
{
    public partial class ОкноСтатистики007 : Window
    {
        private readonly СервисСтатистики007 сервис =
            new();

        private readonly int активныхПК;
        private readonly int всегоПК;

        public ОкноСтатистики007(
            int активныхПК,
            int всегоПК)
        {
            InitializeComponent();

            this.активныхПК = активныхПК;
            this.всегоПК = всегоПК;

            ДатаОт.SelectedDate = DateTime.Today;
            ДатаДо.SelectedDate = DateTime.Today;

            Обновить();
        }

        private void Обновить_Click(
            object sender,
            RoutedEventArgs e)
        {
            Обновить();
        }

        private void Обновить()
        {
            var начало =
                (ДатаОт.SelectedDate ?? DateTime.Today)
                .Date;

            var конец =
                (ДатаДо.SelectedDate ?? DateTime.Today)
                .Date
                .AddDays(1);

            var статистика =
                сервис.Получить(
                    начало,
                    конец);

            Выручка.Text =
                $"{статистика.Выручка:0.##} ₽";

            Сеансы.Text =
                статистика.Сеансов.ToString();

            ИгровоеВремя.Text =
                статистика.ИгровоеВремя.ToString(
                    @"d\.hh\:mm\:ss");

            СреднийЧек.Text =
                $"{статистика.СреднийЧек:0.##} ₽";

            Таблица.ItemsSource =
                сервис.ПолучитьПоПК(
                    начало,
                    конец);
        }

        private void ПрименитьКорректировку_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (!decimal.TryParse(
                    ПолеКорректировкаДенег.Text,
                    out var деньги))
            {
                MessageBox.Show(
                    "Некорректная сумма денег.");

                return;
            }

            if (!int.TryParse(
                    ПолеКорректировкаСеансов.Text,
                    out var сеансы))
            {
                MessageBox.Show(
                    "Некорректное количество сеансов.");

                return;
            }

            if (!int.TryParse(
                    ПолеКорректировкаМинут.Text,
                    out var минуты))
            {
                MessageBox.Show(
                    "Некорректное количество минут.");

                return;
            }

            var дата =
                ДатаОт.SelectedDate?
                    .Date
                ?? DateTime.Today;

            сервис.ДобавитьКорректировку(
                дата,
                деньги,
                сеансы,
                TimeSpan.FromMinutes(минуты),
                ПолеПримечание.Text);

            ПолеКорректировкаДенег.Clear();
            ПолеКорректировкаСеансов.Text = "0";
            ПолеКорректировкаМинут.Text = "0";
            ПолеПримечание.Clear();

            Обновить();
        }

        private void УдалитьКорректировки_Click(
            object sender,
            RoutedEventArgs e)
        {
            var начало =
                (ДатаОт.SelectedDate ?? DateTime.Today)
                .Date;

            var конец =
                (ДатаДо.SelectedDate ?? DateTime.Today)
                .Date
                .AddDays(1);

            var результат =
                MessageBox.Show(
                    "Удалить все ручные корректировки за выбранный период?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (результат != MessageBoxResult.Yes)
                return;

            сервис.УдалитьВсеКорректировки(
                начало,
                конец);

            Обновить();
        }
    }
}