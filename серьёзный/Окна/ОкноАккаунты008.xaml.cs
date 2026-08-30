using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using серьёзный.Модели;
using серьёзный.Сервисы;

namespace серьёзный.Окна
{
    public partial class ОкноАккаунты008 : Window
    {
        private readonly СервисАккаунтов сервис =
            new();

        // =========================================================
        // СОБЫТИЕ ОТКРЫТИЯ ЛИЧНОГО ЧАТА
        // =========================================================

        public event Action<АккаунтИгрока>? ЗапросЧата;

        public ОкноАккаунты008()
        {
            InitializeComponent();

            Обновить();
        }

        // =========================================================
        // ОБНОВЛЕНИЕ СПИСКА
        // =========================================================

        private void Обновить()
        {
            Таблица.ItemsSource =
                сервис.Все
                    .OrderBy(x => x.Имя)
                    .ToList();
        }

        // =========================================================
        // ПОИСК
        // =========================================================

        private void Поиск_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            var текст =
                (ПолеПоиска.Text ?? string.Empty)
                    .Trim();

            var список =
                сервис.Все
                    .Where(x =>
                        string.IsNullOrWhiteSpace(текст) ||
                        x.Имя.Contains(
                            текст,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Имя)
                    .ToList();

            Таблица.ItemsSource =
                список;
        }

        // =========================================================
        // ОТКРЫТЬ КАРТОЧКУ
        // =========================================================

        private void ОткрытьАккаунт(
            object sender,
            MouseButtonEventArgs e)
        {
            if (Таблица.SelectedItem is not АккаунтИгрока аккаунт)
                return;

            var окно =
                new ОкноАккаунта007(аккаунт)
                {
                    Owner = this
                };

            окно.УдалитьАккаунт += id =>
            {
                сервис.Удалить(id);
                Обновить();
            };

            окно.ShowDialog();

            Обновить();
        }

        // =========================================================
        // ОТКРЫТЬ ЛИЧНЫЙ ЧАТ
        // =========================================================

        private void Написать_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Таблица.SelectedItem is not АккаунтИгрока аккаунт)
            {
                MessageBox.Show(
                    "Сначала выберите аккаунт.",
                    "Личный чат",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            ЗапросЧата?.Invoke(аккаунт);
        }

        // =========================================================
        // СОЗДАНИЕ АККАУНТА
        // =========================================================

        private void Создать_Click(
            object sender,
            RoutedEventArgs e)
        {
            var окноИмя =
                new ОкноВвода("Имя игрока")
                {
                    Owner = this
                };

            if (окноИмя.ShowDialog() != true)
                return;

            var имя =
                окноИмя.Текст.Trim();

            if (string.IsNullOrWhiteSpace(имя))
            {
                MessageBox.Show(
                    "Введите имя.",
                    "Создание аккаунта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var окноПароль =
                new ОкноВвода("Пароль аккаунта")
                {
                    Owner = this
                };

            if (окноПароль.ShowDialog() != true)
                return;

            var пароль =
                окноПароль.Текст.Trim();

            if (string.IsNullOrWhiteSpace(пароль))
            {
                MessageBox.Show(
                    "Введите пароль.",
                    "Создание аккаунта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!сервис.Создать(
                    имя,
                    пароль,
                    out var ошибка))
            {
                MessageBox.Show(
                    ошибка,
                    "Создание аккаунта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            ПолеПоиска.Clear();

            Обновить();
        }



        private void ОткрытьЧат_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (sender is not Button кнопка)
                return;

            if (кнопка.Tag is not АккаунтИгрока аккаунт)
                return;

            var окно = new ОкноЧата
            {
                Owner = this
            };

            окно.ИмяАдминистратора = "Администратор";

            окно.УстановитьЛичныйЧат(
                аккаунт.Id,
                аккаунт.Имя);

            окно.Show();
        }
    }
}