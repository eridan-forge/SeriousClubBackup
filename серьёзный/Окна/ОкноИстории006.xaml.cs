using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using серьёзный.Модели;
using серьёзный.Сервисы;

namespace серьёзный.Окна
{
    public partial class ОкноИстории006 : Window
    {
        private readonly СервисАрхива005 архив =
            new();

        private List<ЗаписьАварии005> данные =
            new();

        public ОкноИстории006()
        {
            InitializeComponent();

            Таблица.MouseDoubleClick += ОткрытьАккаунт;

            Обновить();

            УдалитьЗапись.Click += (_, _) =>
            {
                if (Таблица.SelectedItem is not ЗаписьАварии005 запись)
                    return;

                var ответ =
                    MessageBox.Show(
                        "Удалить выбранную запись?",
                        "Подтверждение",
                        MessageBoxButton.YesNo);

                if (ответ != MessageBoxResult.Yes)
                    return;

                архив.УдалитьЗапись(запись.Id);
                Обновить();
            };

            ОчиститьВсё.Click += (_, _) =>
            {
                var ответ =
                    MessageBox.Show(
                        "Полностью очистить весь архив истории? Действие необратимо.",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                if (ответ != MessageBoxResult.Yes)
                    return;

                архив.УдалитьВсё();
                Обновить();
            };
        }



        private void ОткрытьАккаунт(
    object sender,
    MouseButtonEventArgs e)
        {
            if (Таблица.SelectedItem is not ЗаписьАварии005 запись)
                return;

            if (!запись.АккаунтGuid.HasValue)
                return;

            var сервис =
                new СервисАккаунтов();

            var аккаунт =
                сервис.Все.FirstOrDefault(x =>
                    x.Id == запись.АккаунтGuid.Value);

            if (аккаунт == null)
                return;

            var окно =
                new ОкноАккаунта007(аккаунт);

            окно.Owner = this;

            окно.УдалитьАккаунт += id =>
            {
                сервис.Удалить(id);
                Обновить();
            };

            окно.ShowDialog();
        }

        private void Обновить()
        {
            данные =
                архив.ПолучитьВсе();

            ПрименитьФильтр();
        }

        

        private void Поиск_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ПрименитьФильтр();
        }

        private void Фильтр_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            ПрименитьФильтр();
        }

        private void ПрименитьФильтр()
        {

            if (Таблица == null || Фильтр.SelectedItem == null)
                return;

            IEnumerable<ЗаписьАварии005> список =
                данные;

            var поиск =
                ПолеПоиска.Text.Trim();

            if (!string.IsNullOrWhiteSpace(поиск))
            {
                список =
                    список.Where(x =>
                        x.Игрок.Contains(
                            поиск,
                            StringComparison.OrdinalIgnoreCase));
            }

            var выбран =
                ((ComboBoxItem)Фильтр.SelectedItem).Content.ToString();

            var сегодня =
                DateTime.Today;

            switch (выбран)
            {
                case "Сегодня":

                    список =
                        список.Where(x =>
                            x.Отключение.Date == сегодня);
                    break;

                case "Неделя":

                    список =
                        список.Where(x =>
                            x.Отключение >= DateTime.Now.AddDays(-7));
                    break;

                case "Месяц":

                    список =
                        список.Where(x =>
                            x.Отключение >= DateTime.Now.AddMonths(-1));
                    break;
            }

            Таблица.ItemsSource =
                список.ToList();
        }


    }
}