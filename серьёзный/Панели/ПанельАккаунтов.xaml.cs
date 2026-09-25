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
        Таблица.ItemsSource =
            сервис.Искать(ПолеПоиска.Text ?? string.Empty);
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
        var окно =
            new ОкноСозданияАккаунта
            {
                Owner = Window.GetWindow(this)
            };

        if (окно.ShowDialog() != true)
            return;

        ПолеПоиска.Clear();

        Обновить();
    }

    private void СброситьПароль_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button кнопка)
            return;

        if (кнопка.Tag is not АккаунтИгрока аккаунт)
            return;

        var окно =
            new ОкноВвода($"Новый пароль для {аккаунт.ПолноеИмя}")
            {
                Owner = Window.GetWindow(this)
            };

        if (окно.ShowDialog() != true)
            return;

        var новыйПароль = окно.Текст.Trim();

        if (string.IsNullOrWhiteSpace(новыйПароль))
        {
            MessageBox.Show(
                "Пароль не может быть пустым.",
                "Сброс пароля",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        сервис.ИзменитьПароль(аккаунт.Id, новыйПароль);

        MessageBox.Show(
            "Пароль обновлён.",
            "Готово",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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