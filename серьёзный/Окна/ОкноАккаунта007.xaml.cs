using System;
using System.Linq;
using System.Windows;
using серьёзный.Модели;
using серьёзный.Сервисы;
using серьёзный.Core.CoreEconomy;

namespace серьёзный.Окна
{
    public partial class ОкноАккаунта007 : Window
    {
        public event Action<Guid>? УдалитьАккаунт;

        private readonly PointsService баллы = new();
        private readonly PremiumService premium = new();
        private readonly АккаунтИгрока аккаунт;
        private readonly СервисАрхива005 архив = new();
        private readonly СервисАккаунтов сервисАккаунтов = new();
        private readonly СервисЧата сервисЧата = new();

        public ОкноАккаунта007(АккаунтИгрока аккаунт)
        {
            InitializeComponent();

            this.аккаунт = аккаунт;

            Изменить.Click += ИзменитьВремя_Click;
            Баллы.Click += Баллы_Click;
            Удалить.Click += Удалить_Click;
            ОчиститьЧат.Click += ОчиститьЧат_Click;

            ОбновитьКарточку();
            ЗагрузитьИсторию();
        }

        private void ЗагрузитьИсторию()
        {
            Таблица.ItemsSource =
                архив.ПолучитьВсе()
                     .Where(x => x.АккаунтGuid == аккаунт.Id)
                     .OrderByDescending(x => x.Начало)
                     .ToList();
        }

        private void ОбновитьКарточку()
        {
            Имя.Text = аккаунт.Имя;

            Осталось.Text =
                Формат(аккаунт.ОсталосьВремени);

            Сыграно.Text =
                Формат(аккаунт.ВсегоСыграно);

            Сеансов.Text =
                аккаунт.ВсегоСеансов.ToString();

            Последний.Text =
                аккаунт.ПоследнийСеанс == null
                    ? "Последний сеанс: нет истории"
                    : $"Последний сеанс: {аккаунт.ПоследнийСеанс:dd.MM.yyyy HH:mm}";
        }


               private void Баллы_Click(object? sender, RoutedEventArgs e)
       {
           var текущий = баллы.Get(аккаунт.Id);
            var isPremium = premium.IsPremium(аккаунт.Id);

            var окно = new ОкноВвода(
$"Баланс: {текущий.Points} баллов" +
(isPremium ? " • ПРЕМИУМ" : "") +
"\n\nВведите изменение (+50 / -20) или новое значение (=100):",
"");

           if (окно.ShowDialog() != true)
                return;

            var ввод = окно.Текст.Trim();

            if (ввод.StartsWith("="))
            {
                if (long.TryParse(ввод[1..], out var значение))
                    баллы.SetExact(аккаунт.Id, значение, "Ручная корректировка админом");
            }
           else if (long.TryParse(ввод, out var дельта))
            {
                баллы.Award(аккаунт.Id, дельта, "Ручная корректировка админом");
            }
        }

        private void ОчиститьЧат_Click(
            object? sender,
            RoutedEventArgs e)
        {
            var ответ =
                MessageBox.Show(
                    "Удалить всю историю чата этого аккаунта?",
                    "Очистить чат",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (ответ != MessageBoxResult.Yes)
                return;

            сервисЧата.УдалитьИсториюПоАккаунту(аккаунт.Id);

            MessageBox.Show(
                "История чата очищена.",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ИзменитьВремя_Click(
            object? sender,
            RoutedEventArgs e)
        {
            var окно =
                new ОкноВвода(
                    "Остаток времени (минуты)",
                    ((int)аккаунт.ОсталосьВремени.TotalMinutes).ToString());

            окно.Owner = this;

            if (окно.ShowDialog() != true)
                return;

            if (!int.TryParse(окно.Текст.Trim(), out var минуты))
            {
                MessageBox.Show(
                    "Введите число.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (минуты < 0)
                минуты = 0;

            var новоеВремя =
                TimeSpan.FromMinutes(минуты);

            сервисАккаунтов.УстановитьОстаток(
                аккаунт.Id,
                новоеВремя);

            аккаунт.ОсталосьВремени = новоеВремя;

            ОбновитьКарточку();
        }

        private void Удалить_Click(
            object? sender,
            RoutedEventArgs e)
        {
            var ответ =
                MessageBox.Show(
                    $"Удалить аккаунт «{аккаунт.Имя}»?\n\nУдалится также история сеансов и история чата.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (ответ != MessageBoxResult.Yes)
                return;

            архив.УдалитьПоАккаунту(аккаунт.Id);
            сервисЧата.УдалитьИсториюПоАккаунту(аккаунт.Id);

            УдалитьАккаунт?.Invoke(аккаунт.Id);

            Close();
        }

        private static string Формат(TimeSpan время)
        {
            if (время < TimeSpan.Zero)
                время = TimeSpan.Zero;

            if (время.TotalHours >= 100)
                return $"{(int)время.TotalHours}:{время.Minutes:00}";

            return время.ToString(@"hh\:mm");
        }
    }
}