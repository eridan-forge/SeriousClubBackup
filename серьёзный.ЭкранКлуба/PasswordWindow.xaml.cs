using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using серьёзный.Core.CoreAudit;
using серьёзный.Core.CoreThemes;
using серьёзный.ЭкранКлуба.Сервисы;

namespace серьёзный.ЭкранКлуба;

public partial class ОкноСменыОбоев : Window
{
    private readonly AdminActionLogService лог = new();

    private readonly int idПК;

    public ОкноСменыОбоев()
    {
        InitializeComponent();

        try { idПК = StateService.Загрузить().PcId; }
        catch { idПК = 0; }

        Loaded += (_, _) => ПостроитьСписок();
    }

    private void ПостроитьСписок()
    {
        СписокТем.Children.Clear();

        var темы = ЗагрузчикТем.ЗагрузитьВсе();

        if (темы.Count == 0)
        {
            СписокТем.Children.Add(new TextBlock
            {
                Text = "Тем не найдено. Проверь Темы\\ рядом с программой или " +
                       "%ProgramData%\\SeriousClub\\Themes\\.",
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap
            });

            return;
        }

        foreach (var тема in темы)
        {
            СписокТем.Children.Add(СоздатьКарточку(тема));
        }
    }

    private Border СоздатьКарточку(ТемаВхода тема)
    {
        var превью = new Border
        {
            Width = 46,
            Height = 46,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(36, 36, 41)),
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Center
        };

        DockPanel.SetDock(превью, Dock.Left);

        if (File.Exists(тема.ПутьЛого))
        {
            превью.Child = new Image
            {
                Source = new BitmapImage(new Uri(тема.ПутьЛого)),
                Stretch = Stretch.Uniform,
                Margin = new Thickness(6)
            };
        }

        var название = new TextBlock
        {
            Text = тема.Id,
            Foreground = Brushes.White,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        };

        var тип = new TextBlock
        {
            Text = тема.ФонЭтоВидео
                ? $"🎬 видео × {тема.Видео.Count}"
                : "🖼 фото",
            Foreground = Brushes.Gray,
            FontSize = 12,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var текстоваяКолонка = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 0, 0)
        };

        текстоваяКолонка.Children.Add(название);
        текстоваяКолонка.Children.Add(тип);

        var содержимое = new DockPanel();
        содержимое.Children.Add(превью);
        содержимое.Children.Add(текстоваяКолонка);

        var карточка = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(26, 17, 22)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(58, 34, 43)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10),
            Cursor = Cursors.Hand,
            Child = содержимое
        };

        карточка.MouseLeftButtonUp += (_, _) =>
        {
            серьёзный.Патруль.Сервисы.СервисЭкранаКлуба.УстановитьТему(тема.Id);

            try
            {
                лог.Log(
                    "Смена обоев",
                    $"Тема «{тема.Id}», видео в плейлисте: {тема.Видео.Count}",
                    $"Обслуживание ПК-{idПК}");
            }
            catch
            {
            }

            DialogResult = true;
        };

        return карточка;
    }

    private void Закрыть_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}