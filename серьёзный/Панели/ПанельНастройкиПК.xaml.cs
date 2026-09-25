using System;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Core.CoreComputers;

namespace серьёзный.Панели;

public partial class ПанельНастройкиПК : UserControl
{
    public event Action? Закрыть;

    public event Action? Изменено;

    public ПанельНастройкиПК()
    {
        InitializeComponent();
        Обновить();
    }

    private void Обновить()
    {
        Список.ItemsSource = null;
        Список.ItemsSource = КартаКомпьютеров.Все;
    }

    private void Назад_Click(object sender, RoutedEventArgs e)
    {
        Закрыть?.Invoke();
    }

    private void Список_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Список.SelectedItem is not ЗаписьПК пк)
            return;

        ПолеId.Text = пк.Id.ToString();
        ПолеНазвание.Text = пк.Название;
        ПолеMAC.Text = пк.MAC;
    }

    private void Добавить_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ПолеId.Text, out var id))
        {
            MessageBox.Show("Введите корректный Id.");
            return;
        }

        try
        {
            КартаКомпьютеров.Добавить(id, ПолеНазвание.Text.Trim(), ПолеMAC.Text.Trim());
            Обновить();
            Изменено?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.GetType().Name + ": " + ex.Message);
        }
    }

    private void Сохранить_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ПолеId.Text, out var id))
        {
            MessageBox.Show("Введите корректный Id.");
            return;
        }

        try
        {
            КартаКомпьютеров.Изменить(id, ПолеНазвание.Text.Trim(), ПолеMAC.Text.Trim());
            Обновить();
            Изменено?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.GetType().Name + ": " + ex.Message);
        }
    }

    private void Удалить_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ПолеId.Text, out var id))
            return;

        КартаКомпьютеров.Удалить(id);
        Обновить();
        Изменено?.Invoke();
    }
}