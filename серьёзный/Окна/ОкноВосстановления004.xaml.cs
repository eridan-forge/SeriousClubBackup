using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using серьёзный.Модели;

namespace серьёзный.Окна
{
    public partial class ОкноВосстановления004 : Window
    {
        public ObservableCollection<Сеанс004> Сеансы =
            new();

        public event Action<Сеанс004>? Возобновить;

        public event Action<Сеанс004>? Завершить;

        public event Action? ЗавершитьВсе;

        public ОкноВосстановления004(
            СнимокСеансов001 снимок)
        {
            InitializeComponent();

            foreach (var x in снимок.Сеансы)
            {
                Сеансы.Add(
    new Сеанс004
    {
        Id = x.Id,
        КомпьютерId = x.КомпьютерId,
        Название = $"PC-{x.КомпьютерId}",
        Игрок = x.ИмяКлиента,
        Осталось =
            x.ВремяАккаунта +
            x.КупленноеВремя,

        АккаунтGuid =
            x.АккаунтGuid,

        Начало =
            x.Начало,

        Стоимость =
            x.Стоимость
    });
            }

            Список.ItemsSource = Сеансы;
        }

        private void Возобновить_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is not Сеанс004 сеанс)
                return;

            Возобновить?.Invoke(сеанс);

            Сеансы.Remove(сеанс);
        }

        private void Завершить_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is not Сеанс004 сеанс)
                return;

            Завершить?.Invoke(сеанс);

            Сеансы.Remove(сеанс);
        }

        private void ЗавершитьВсе_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Вернуть время всем аккаунтам и завершить все сеансы?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question)
                != MessageBoxResult.Yes)
            {
                return;
            }

            ЗавершитьВсе?.Invoke();

            Close();
        }
    }

    public class Сеанс004
    {
        public int Id { get; set; }

        public Guid? АккаунтGuid { get; set; }

        public DateTime Начало { get; set; }

        public decimal Стоимость { get; set; }

        public int КомпьютерId { get; set; }

        public string Название { get; set; } = "";

        public string Игрок { get; set; } = "";

        public TimeSpan Осталось { get; set; }

        public string ОсталосьСтрокой =>
            $"Осталось: {Осталось:hh\\:mm}";
    }
}