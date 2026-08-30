using System;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Модели;
using серьёзный.Сервисы;

namespace серьёзный.Окна
{
    public partial class ОкноСеанса001 : Window
    {
        private readonly СервисАккаунтов сервис = new();

        private bool обновляетсяВыбранныйАккаунт;

        public АккаунтИгрока? Аккаунт { get; private set; }

        public bool ИспользоватьБалансАккаунта { get; private set; }

        public int Минуты =>
            int.TryParse(ПолеМинут.Text, out var m) ? m : 0;

        public decimal Стоимость =>
            decimal.TryParse(ПолеСтоимость.Text, out var s) ? s : -1m;

        public ОкноСеанса001(string названиеПК)
        {
            InitializeComponent();

            ПК.Text = string.IsNullOrWhiteSpace(названиеПК)
                ? "ПК"
                : названиеПК;

            ПанельПароля.Visibility = Visibility.Visible;
        }

        //=========================================================
        // ПОИСК АККАУНТА
        //=========================================================

        private void ПоискАккаунта(object sender, TextChangedEventArgs e)
        {
            if (обновляетсяВыбранныйАккаунт)
                return;

            Аккаунт = null;
            ИспользоватьБалансАккаунта = false;

            КарточкаАккаунта.Visibility = Visibility.Collapsed;
            ПанельПароля.Visibility = Visibility.Visible;

            var имя = (ПолеИмя.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(имя))
            {
                ОчиститьСписок();
                return;
            }

            var список = сервис.Искать(имя);

            if (список.Count == 0)
            {
                ОчиститьСписок();
                return;
            }

            СписокСовпадений.ItemsSource = список;
            СписокСовпадений.SelectedItem = null;
            СписокСовпадений.Visibility = Visibility.Visible;
        }

        private void ОчиститьСписок()
        {
            СписокСовпадений.ItemsSource = null;
            СписокСовпадений.SelectedItem = null;
            СписокСовпадений.Visibility = Visibility.Collapsed;
        }

        private void ПарольИзменён(object sender, RoutedEventArgs e)
        {
        }

        //=========================================================
        // ВЫБОР АККАУНТА
        //=========================================================

        private void ВыбранАккаунт(object sender, SelectionChangedEventArgs e)
        {
            if (СписокСовпадений.SelectedItem is not АккаунтИгрока выбранный)
                return;

            обновляетсяВыбранныйАккаунт = true;

            try
            {
                Аккаунт = выбранный;
                ПолеИмя.Text = выбранный.Имя;
                ПолеПароль.Clear();
            }
            finally
            {
                обновляетсяВыбранныйАккаунт = false;
            }

            ОчиститьСписок();

            ПанельПароля.Visibility = Visibility.Collapsed;
            КарточкаАккаунта.Visibility = Visibility.Visible;

            ИмяАккаунта.Text = выбранный.ПолноеИмя;
            ТекстОстатка001.Text = "Осталось: " + Формат(выбранный.ОсталосьВремени);
            ТекстСтатистики001.Text =
                $"{выбранный.ВсегоСеансов} сеансов • {Формат(выбранный.ВсегоСыграно)}";
        }

        //=========================================================
        // НАЧАТЬ СЕАНС
        //=========================================================

        private void Начать(object sender, RoutedEventArgs e)
        {
            var имя = (ПолеИмя.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(имя))
            {
                MessageBox.Show("Введите имя.");
                ПолеИмя.Focus();
                return;
            }

            if (Минуты <= 0)
            {
                MessageBox.Show("Введите корректное количество минут.");
                ПолеМинут.Focus();
                return;
            }

            if (Стоимость < 0)
            {
                MessageBox.Show("Введите корректную стоимость.");
                ПолеСтоимость.Focus();
                return;
            }

            // Новый аккаунт
            if (Аккаунт == null)
            {
                var пароль = (ПолеПароль.Password ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(пароль))
                {
                    MessageBox.Show("Для нового аккаунта необходимо указать пароль.");
                    ПолеПароль.Focus();
                    return;
                }

                if (!сервис.Создать(имя, пароль, out var ошибка))
                {
                    MessageBox.Show(ошибка);
                    return;
                }

                Аккаунт = сервис.Найти(имя);

                if (Аккаунт == null)
                {
                    MessageBox.Show("Не удалось открыть созданный аккаунт.");
                    return;
                }
            }

            ИспользоватьБалансАккаунта = false;

            var остаток = Аккаунт.ОсталосьВремени;

            if (остаток > TimeSpan.Zero)
            {
                var выбор = new ОкноОстатка002(
                    Аккаунт.ПолноеИмя,
                    остаток)
                {
                    Owner = this
                };

                if (выбор.ShowDialog() != true)
                    return;

                if (выбор.ИспользоватьОстаток)
                {
                    ИспользоватьБалансАккаунта = true;

                    ПолеМинут.Text =
                        Math.Max(1, (int)остаток.TotalMinutes)
                        .ToString();
                }
                else
                {
                    var объединить = new ОкноОбъединения003(
                        TimeSpan.FromMinutes(Минуты),
                        остаток)
                    {
                        Owner = this
                    };

                    if (объединить.ShowDialog() != true)
                        return;

                    if (объединить.ДобавитьОстаток)
                    {
                        ИспользоватьБалансАккаунта = true;

                        ПолеМинут.Text =
                            (Минуты + (int)остаток.TotalMinutes)
                            .ToString();
                    }
                }
            }

            DialogResult = true;
        }

        //=========================================================
        // ФОРМАТ ВРЕМЕНИ
        //=========================================================

        private static string Формат(TimeSpan время)
        {
            if (время < TimeSpan.Zero)
                время = TimeSpan.Zero;

            if (время.TotalHours >= 100)
                return $"{(int)время.TotalHours}:{время.Minutes:00}";

            return время.ToString(@"hh\:mm");
        }
    }
}