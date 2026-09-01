using System.Diagnostics;
using System.Windows;
using серьёзный.ЭкранКлуба.Сервисы;

namespace серьёзный.ЭкранКлуба
{
    public partial class PasswordWindow : Window
    {
        private readonly string пароль;

        public PasswordWindow(string пароль)
        {
            InitializeComponent();

            this.пароль = пароль;
        }

        private void Войти_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ПолеПароля.Password != пароль)
            {
                ТекстОшибки.Text =
                    "Неверный пароль.";
                return;
            }

            ВходПанель.Visibility =
                Visibility.Collapsed;

            ПанельАдмина.Visibility =
                Visibility.Visible;
        }

        private void Запустить_Click(
    object sender,
    RoutedEventArgs e)
        {
            var state = StateService.Загрузить();

            state.Locked = false;

            StateService.Сохранить(state);

            DialogResult = true;
        }

        private void Заблокировать_Click(
     object sender,
     RoutedEventArgs e)
        {
            var state = StateService.Загрузить();

            state.Locked = true;

            StateService.Сохранить(state);

            DialogResult = false;
        }

        private void СменитьПароль_Click(
            object sender,
            RoutedEventArgs e)
        {
            var окно =
                new серьёзный.ОкноВвода(
                    "Новый пароль обслуживания");

            if (окно.ShowDialog() != true)
                return;

            var новыйПароль =
                окно.Текст.Trim();

            if (string.IsNullOrWhiteSpace(новыйПароль))
            {
                MessageBox.Show("Пароль не может быть пустым.");
                return;
            }

            серьёзный.Патруль.Сервисы.СервисЭкранаКлуба
                .СменитьПароль(новыйПароль);

            MessageBox.Show(
                "Пароль обслуживания изменён.",
                "Готово");
        }

        private void ИзменитьТекст_Click(
            object sender,
            RoutedEventArgs e)
        {
            var текущий =
                ConfigService.Загрузить();

            var окно =
                new серьёзный.ОкноВвода(
                    "Текст на экране клуба",
                    текущий.Title);

            if (окно.ShowDialog() != true)
                return;

            серьёзный.Патруль.Сервисы.СервисЭкранаКлуба
                .ИзменитьТекст(окно.Текст.Trim());

            MessageBox.Show(
                "Текст экрана изменён.",
                "Готово");
        }

        private void Выключить_Click(
            object sender,
            RoutedEventArgs e)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/s /t 0",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
        }
    }
}