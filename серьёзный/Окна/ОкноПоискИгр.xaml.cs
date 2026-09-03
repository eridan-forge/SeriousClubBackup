using System.Collections.Generic;
using System.Linq;
using System.Windows;
using серьёзный.Core.CoreModels;

namespace серьёзный.Окна;

public partial class ОкноПоискИгр : Window
{
    public class ЭлементВыбора
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Path { get; set; } = "";
        public bool Выбрана { get; set; } = true;
        public GameEntry Источник { get; set; } = null!;
    }

    private readonly List<ЭлементВыбора> элементы;

    public List<GameEntry> Выбранные { get; private set; } = new();

    public ОкноПоискИгр(List<GameEntry> найденные)
    {
        InitializeComponent();

        элементы = найденные
            .Select(x => new ЭлементВыбора
            {
                Name = x.Name,
                Category = x.Category,
                Path = x.Path,
                Источник = x
            })
            .ToList();

        Список.ItemsSource = элементы;
    }

    private void Добавить_Click(object sender, RoutedEventArgs e)
    {
        Выбранные = элементы
            .Where(x => x.Выбрана)
            .Select(x => x.Источник)
            .ToList();

        if (Выбранные.Count == 0)
        {
            MessageBox.Show("Отметьте хотя бы одну игру.");
            return;
        }

        DialogResult = true;
    }

    private void Отмена_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}