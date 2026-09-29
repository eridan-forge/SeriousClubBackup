using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Сервисы;

namespace серьёзный.Панели;

public partial class ПанельСтатистики : UserControl
{
    private readonly СервисСтатистики007 сервис = new();

    private readonly int активныхПК;
    private readonly int всегоПК;

    public event Action? Закрыть;

    public ПанельСтатистики(int активныхПК, int всегоПК)
    {
        InitializeComponent();

        this.активныхПК = активныхПК;
        this.всегоПК = всегоПК;

        ДатаОт.SelectedDate = DateTime.Today;
        ДатаДо.SelectedDate = DateTime.Today;
        ДатаКорректировки.SelectedDate = DateTime.Today;

        Обновить();
    }

    private void Назад_Click(object sender, RoutedEventArgs e) => Закрыть?.Invoke();

    private void Обновить_Click(object sender, RoutedEventArgs e) => Обновить();

    private (DateTime Начало, DateTime Конец) Период()
    {
        var начало = (ДатаОт.SelectedDate ?? DateTime.Today).Date;
        var конец = (ДатаДо.SelectedDate ?? DateTime.Today).Date.AddDays(1);

        return (начало, конец);
    }

    private void Обновить()
    {
        var (начало, конец) = Период();

        var статистика = сервис.Получить(начало, конец);

        Выручка.Text = $"{статистика.Выручка:0.##} ₽";
        Сеансы.Text = статистика.Сеансов.ToString();
        ИгровоеВремя.Text = статистика.ИгровоеВремя.ToString(@"d\.hh\:mm\:ss");
        СреднийЧек.Text = $"{статистика.СреднийЧек:0.##} ₽";

        Таблица.ItemsSource = сервис.ПолучитьПоПК(начало, конец);
        ТаблицаКорректировок.ItemsSource = сервис.ПолучитьКорректировки(начало, конец);
    }

    private void ДобавитьКорректировку_Click(object sender, RoutedEventArgs e) =>
        Применить(+1);

    private void ВычестьКорректировку_Click(object sender, RoutedEventArgs e) =>
        Применить(-1);

    private void Применить(int знак)
    {
        if (!ПрочитатьДецимал(ПолеКорректировкаДенег.Text, out var деньги))
        {
            MessageBox.Show("Некорректная сумма денег.");
            return;
        }

        if (!int.TryParse(ПолеКорректировкаСеансов.Text.Trim(), out var сеансы))
        {
            MessageBox.Show("Некорректное количество сеансов.");
            return;
        }

        if (!int.TryParse(ПолеКорректировкаМинут.Text.Trim(), out var минуты))
        {
            MessageBox.Show("Некорректное количество минут.");
            return;
        }

        деньги = Math.Abs(деньги);
        сеансы = Math.Abs(сеансы);
        минуты = Math.Abs(минуты);

        if (деньги == 0 && сеансы == 0 && минуты == 0)
        {
            MessageBox.Show("Введите хотя бы одно значение больше нуля.");
            return;
        }

        var дата = (ДатаКорректировки.SelectedDate ?? DateTime.Today).Date;

        сервис.ДобавитьКорректировку(
            дата,
            деньги * знак,
            сеансы * знак,
            TimeSpan.FromMinutes(минуты * знак),
            ПолеПримечание.Text.Trim());

        ПолеКорректировкаДенег.Text = "0";
        ПолеКорректировкаСеансов.Text = "0";
        ПолеКорректировкаМинут.Text = "0";
        ПолеПримечание.Clear();

        // Если дата корректировки вне выбранного периода — расширяем период,
        // иначе итог не изменится и будет казаться, что кнопка не сработала.
        if (дата < (ДатаОт.SelectedDate ?? DateTime.Today).Date)
            ДатаОт.SelectedDate = дата;

        if (дата > (ДатаДо.SelectedDate ?? DateTime.Today).Date)
            ДатаДо.SelectedDate = дата;

        Обновить();
    }

    private static bool ПрочитатьДецимал(string текст, out decimal значение)
    {
        текст = (текст ?? "").Trim().Replace(',', '.');

        return decimal.TryParse(
            текст,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out значение);
    }

    private void УдалитьОдну_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button кнопка || кнопка.Tag is not long id)
            return;

        сервис.УдалитьКорректировку(id);

        Обновить();
    }

    private void УдалитьКорректировки_Click(object sender, RoutedEventArgs e)
    {
        var (начало, конец) = Период();

        var результат = MessageBox.Show(
            "Удалить все ручные корректировки за выбранный период?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (результат != MessageBoxResult.Yes)
            return;

        сервис.УдалитьВсеКорректировки(начало, конец);

        Обновить();
    }
}