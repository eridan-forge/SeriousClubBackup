using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using серьёзный.Core.CoreAudit;
using серьёзный.Core.CoreThemes;
using серьёзный.ЭкранКлуба.Сервисы;

namespace серьёзный.ЭкранКлуба
{
    public partial class PasswordWindow : Window
    {
        private readonly string пароль;

        private readonly AdminActionLogService лог = new();

        private readonly int idПК;

        private const double ВысотаОкнаАдмина = 700;

        public PasswordWindow(string пароль)
        {
            InitializeComponent();

            this.пароль = пароль;

            try { idПК = StateService.Загрузить().PcId; }
            catch { idПК = 0; }

            Loaded += (_, _) => ПолеПароля.Focus();
        }

        private void Записать(string действие, string детали = "")
        {
            try
            {
                лог.Log(действие, детали, $"Обслуживание ПК-{idПК}");
            }
            catch
            {
            }
        }

        private void Закрыть_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ПолеПароля_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            Войти_Click(this, new RoutedEventArgs());
        }

        private void Войти_Click(object sender, RoutedEventArgs e)
        {
            if (ПолеПароля.Password != пароль)
            {
                ТекстОшибки.Text = "Неверный пароль.";

                Записать("Неудачный вход в обслуживание");

                return;
            }

            Записать("Вход в обслуживание");

            ВходПанель.Visibility = Visibility.Collapsed;
            ПанельАдмина.Visibility = Visibility.Visible;

            РасширитьОкноДляПанелиАдмина();
        }

        // Окно входа — маленькое, без пустого места. После верного
        // пароля раскрываем его до размера, где видно все кнопки
        // обслуживания (плюс скролл внутри — подстраховка на случай
        // маленького экрана, где даже увеличенное окно не влезло бы).
        private void РасширитьОкноДляПанелиАдмина()
        {
            var рабочаяОбласть = SystemParameters.WorkArea;

            var целеваяВысота = Math.Min(
                ВысотаОкнаАдмина,
                Math.Max(360, рабочаяОбласть.Height - 60));

            var стартТоп = Top;
            var стартВысота = ActualHeight > 0 ? ActualHeight : Height;

            var центрY = стартТоп + стартВысота / 2.0;

            var новыйTop = центрY - целеваяВысота / 2.0;

            новыйTop = Math.Max(
                рабочаяОбласть.Top + 10,
                Math.Min(новыйTop, рабочаяОбласть.Bottom - целеваяВысота - 10));

            var сглаживание = new CubicEase { EasingMode = EasingMode.EaseOut };

            var анимВысота = new DoubleAnimation(стартВысота, целеваяВысота, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = сглаживание
            };

            var анимTop = new DoubleAnimation(стартТоп, новыйTop, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = сглаживание
            };

            BeginAnimation(HeightProperty, анимВысота);
            BeginAnimation(TopProperty, анимTop);
        }

        private void Запустить_Click(object sender, RoutedEventArgs e)
        {
            var state = StateService.Загрузить();

            state.Locked = false;

            StateService.Сохранить(state);

            Записать("Запущен рабочий стол");

            DialogResult = true;
        }

        private void Заблокировать_Click(object sender, RoutedEventArgs e)
        {
            var state = StateService.Загрузить();

            state.Locked = true;

            StateService.Сохранить(state);

            Записать("Экран заблокирован вручную");

            DialogResult = false;
        }

        private void СменитьПароль_Click(object sender, RoutedEventArgs e)
        {
            var окно = new серьёзный.ОкноВвода("Новый пароль обслуживания")
            {
                Owner = this,
                Topmost = true
            };

            if (окно.ShowDialog() != true)
                return;

            var новыйПароль = окно.Текст.Trim();

            if (string.IsNullOrWhiteSpace(новыйПароль))
            {
                MessageBox.Show("Пароль не может быть пустым.");
                return;
            }

            серьёзный.Патруль.Сервисы.СервисЭкранаКлуба.СменитьПароль(новыйПароль);

            Записать("Изменён пароль обслуживания");

            MessageBox.Show("Пароль обслуживания изменён.", "Готово");
        }

        private void ИзменитьТекст_Click(object sender, RoutedEventArgs e)
        {
            var текущий = ConfigService.Загрузить();

            var окно = new серьёзный.ОкноВвода("Текст на экране клуба", текущий.Title)
            {
                Owner = this,
                Topmost = true
            };

            if (окно.ShowDialog() != true)
                return;

            var новыйТекст = окно.Текст.Trim();

            серьёзный.Патруль.Сервисы.СервисЭкранаКлуба.ИзменитьТекст(новыйТекст);

            Записать("Изменён текст экрана", новыйТекст);

            MessageBox.Show("Текст экрана изменён.", "Готово");
        }

        private void СменитьОбои_Click(object sender, RoutedEventArgs e)
        {
            new ОкноСменыОбоев
            {
                Owner = this
            }.ShowDialog();
        }

        private void СлучайнаяТема_Click(object sender, RoutedEventArgs e)
        {
            var темы = ЗагрузчикТем.ЗагрузитьВсе();

            if (темы.Count == 0)
            {
                MessageBox.Show(
                    "Тем не найдено. Положи папку темы в " +
                    "%ProgramData%\\SeriousClub\\Themes\\.",
                    "Случайная тема");

                return;
            }

            var текущая = ConfigService.Загрузить().ThemeId;

            var кандидаты = темы
                .Where(x => !string.Equals(x.Id, текущая, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (кандидаты.Count == 0)
                кандидаты = темы;

            var выбранная = кандидаты[Random.Shared.Next(кандидаты.Count)];

            серьёзный.Патруль.Сервисы.СервисЭкранаКлуба.УстановитьТему(выбранная.Id);

            Записать(
                "Смена обоев (случайная)",
                $"Тема «{выбранная.Id}», видео в плейлисте: {выбранная.Видео.Count}");

            MessageBox.Show($"Включена тема «{выбранная.Id}».", "Случайная тема");
        }

        private void История_Click(object sender, RoutedEventArgs e)
        {
            new ОкноИсторииОбслуживания
            {
                Owner = this
            }.ShowDialog();
        }

        private void Выключить_Click(object sender, RoutedEventArgs e)
        {
            Записать("Выключение ПК из обслуживания");

            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = "/s /t 0",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        private void ЗакрытьПриложение_Click(object sender, RoutedEventArgs e)
        {
            var подтверждение = MessageBox.Show(
                "Полностью закрыть приложение «Экран клуба»?\n\n" +
                "На реальном клубном ПК это закроет киоск-режим " +
                "и потребует ручного перезапуска процесса.",
                "Закрыть приложение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (подтверждение != MessageBoxResult.Yes)
                return;

            Записать("Экран клуба закрыт вручную");

            Environment.Exit(0);
        }
    }
}