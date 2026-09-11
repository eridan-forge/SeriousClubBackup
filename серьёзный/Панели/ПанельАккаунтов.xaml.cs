using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using серьёзный.Модели;
using серьёзный.Окна;
using серьёзный.Сервисы;

namespace серьёзный.Панели;

public partial class ПанельАккаунтов : UserControl
{
    private readonly СервисАккаунтов сервис = new();

    public event Action? Закрыть;

    public event Action<АккаунтИгрока>? ЗапросЧата;

    public ПанельАккаунтов()
    {
        InitializeComponent();

        Обновить();
    }

    private void Назад_Click(object sender, RoutedEventArgs e)
    {
        Закрыть?.Invoke();
    }

    private void Обновить()
    {
        Таблица.ItemsSource =
            сервис.Все
                .OrderBy(x => x.Имя)
                .ToList();
    }

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

    private void ОткрытьАккаунт(
        object sender,
        MouseButtonEventArgs e)
    {
        if (Таблица.SelectedItem is not АккаунтИгрока аккаунт)
            return;

        var окно =
            new ОкноАккаунта007(аккаунт)
            {
                Owner = Window.GetWindow(this)
            };

        окно.УдалитьАккаунт += id =>
        {
            сервис.Удалить(id);
            Обновить();
        };

        окно.ShowDialog();

        Обновить();
    }

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

    private void Создать_Click(
        object sender,
        RoutedEventArgs e)
    {
        var окноИмя =
            new ОкноВвода("Имя игрока")
            {
                Owner = Window.GetWindow(this)
            };

        if (окноИмя.ShowDialog() != true)
            return;

        var имя = окноИмя.Текст.Trim();

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
                Owner = Window.GetWindow(this)
            };

        if (окноПароль.ShowDialog() != true)
            return;

        var пароль = окноПароль.Текст.Trim();

        if (string.IsNullOrWhiteSpace(пароль))
        {
            MessageBox.Show(
                "Введите пароль.",
                "Создание аккаунта",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (!сервис.Создать(имя, пароль, out var ошибка))
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
            Owner = Window.GetWindow(this)
        };

        окно.ИмяАдминистратора = "Администратор";

        окно.УстановитьЛичныйЧат(аккаунт.Id, аккаунт.Имя);

        окно.Show();
    }
}