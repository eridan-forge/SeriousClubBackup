using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Core.CoreComputers;
using серьёзный.Core.CoreThemes;

namespace серьёзный.Панели;

public partial class ПанельТемВхода : UserControl
{
    public event Action? Закрыть;

    // Функция, возвращающая Id ПК, выбранного сейчас в главном окне.
    private readonly Func<int?> текущийПК;

    // Переиспользуем уже существующий ОтправитьКомандуAsync из главного
    // окна — никакой новой сетевой логики тут не пишем.
    private readonly Func<int, string, Task> отправитьТемуНаПК;

    public ПанельТемВхода(Func<int?> текущийПК, Func<int, string, Task> отправитьТемуНаПК)
    {
        InitializeComponent();

        this.текущийПК = текущийПК;
        this.отправитьТемуНаПК = отправитьТемуНаПК;

        ОбновитьСписок();
    }

    private void Назад_Click(object sender, RoutedEventArgs e) => Закрыть?.Invoke();

    private void Обновить_Click(object sender, RoutedEventArgs e) => ОбновитьСписок();

    private void ОбновитьСписок()
    {
        var выбранныйId = (СписокТем.SelectedItem as ТемаВхода)?.Id;

        var темы = ЗагрузчикТем.ЗагрузитьВсе();

        СписокТем.ItemsSource = темы;

        СписокТем.SelectedItem =
            темы.FirstOrDefault(x => x.Id == выбранныйId) ?? темы.FirstOrDefault();

        ТекстСтатуса.Text = темы.Count == 0
            ? "Тем не найдено. Проверь папку Темы\\ рядом с программой — в ней должна быть подпапка с bg.mp4, fg.mp4 и logo.png."
            : $"Найдено тем: {темы.Count}.";
    }

    private async void ПрименитьВыбранному_Click(object sender, RoutedEventArgs e)
    {
        if (СписокТем.SelectedItem is not ТемаВхода тема)
        {
            MessageBox.Show("Сначала выбери тему из списка.");
            return;
        }

        var pcId = текущийПК();

        if (pcId == null)
        {
            MessageBox.Show("Сначала выбери ПК справа в главном окне.");
            return;
        }

        НазначенныеТемы.Установить(pcId.Value, тема.Id);

        await отправитьТемуНаПК(pcId.Value, тема.Id);

        ТекстСтатуса.Text = $"Тема «{тема.Id}» отправлена на ПК-{pcId.Value}.";
    }

    private async void ПрименитьВсем_Click(object sender, RoutedEventArgs e)
    {
        if (СписокТем.SelectedItem is not ТемаВхода тема)
        {
            MessageBox.Show("Сначала выбери тему из списка.");
            return;
        }

        foreach (var пк in КартаКомпьютеров.Все)
        {
            НазначенныеТемы.Установить(пк.Id, тема.Id);

            await отправитьТемуНаПК(пк.Id, тема.Id);
        }

        ТекстСтатуса.Text = $"Тема «{тема.Id}» назначена всем ПК ({КартаКомпьютеров.Все.Count}).";
    }
}