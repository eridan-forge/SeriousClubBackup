using System;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Core.CoreCommunity;

namespace серьёзный.Панели;

public partial class ПанельВопросовИгр : UserControl
{
    private readonly GameCommunityService сервис = new();

    public event Action? Закрыть;

    public ПанельВопросовИгр()
    {
        InitializeComponent();
        Обновить();
    }

    private void Назад_Click(object sender, RoutedEventArgs e) => Закрыть?.Invoke();

    private void Обновить_Changed(object sender, RoutedEventArgs e) => Обновить();

    private void Обновить()
    {
        Таблица.ItemsSource = null;
        Таблица.ItemsSource = сервис.GetAllQuestions(ФлагТолькоБезОтвета.IsChecked == true);
    }

    private void Таблица_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ПолеОтвета.Text = (Таблица.SelectedItem as GameHelpQuestionRecord)?.Answer ?? "";
    }

    private void СохранитьОтвет_Click(object sender, RoutedEventArgs e)
    {
        if (Таблица.SelectedItem is not GameHelpQuestionRecord вопрос)
        {
            MessageBox.Show("Сначала выберите вопрос в таблице.");
            return;
        }

        var ответ = ПолеОтвета.Text.Trim();

        if (string.IsNullOrWhiteSpace(ответ))
        {
            MessageBox.Show("Введите текст ответа.");
            return;
        }

        сервис.AnswerQuestion(вопрос.Id, ответ, "Администратор");

        Обновить();
    }
}