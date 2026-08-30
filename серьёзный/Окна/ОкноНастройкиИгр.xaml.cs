using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreServices;
using серьёзный.Модели;
using серьёзный.Сервисы;
using серьёзный.Элементы;

namespace серьёзный.Окна
{
    public partial class ОкноНастройкиИгр : Window
    {
        private readonly СервисИгр сервис = new();

        private readonly int idПК;

        private Игра? текущая;

        public ОкноНастройкиИгр(int idПК)
        {
            InitializeComponent();

            DragDropGameService.Enable(
    this,
    OnExeDropped);

            DragDropCoverService.Enable(
                Обложка,
                OnCoverDropped);

            CardDropService.Enable(
                СеткаКарточек);

            this.idПК = idПК;

            Title = $"Игры ПК-{idПК}";

            CardReorderService.Enable(СеткаКарточек);



            ПостроитьКарточки();
        }

        // =========================================================
        // ПОСТРОЕНИЕ КАРТОЧЕК
        // =========================================================

        private void ПостроитьКарточки()
        {
            СеткаКарточек.Children.Clear();

            foreach (var игра in сервис.ПолучитьИгры(idПК))
            {
                var карточка = new КарточкаИгрыАдмин();

                карточка.Загрузить(игра);

                карточка.Редактировать += ВыбратьИгру;

                СеткаКарточек.Children.Add(карточка);
            }
        }

        // =========================================================
        // ЗАГРУЗКА ИГРЫ В ПРАВУЮ ПАНЕЛЬ
        // =========================================================

        private void ВыбратьИгру(Игра игра)
        {
            текущая = игра;

            Название.Text = игра.Название;
            Категория.Text = игра.Категория;
            Описание.Text = игра.Описание;
            Путь.Text = игра.Путь;
            Скрыта.IsChecked = игра.Скрыта;

            if (!string.IsNullOrWhiteSpace(игра.Обложка) &&
                File.Exists(игра.Обложка))
            {
                Обложка.Source =
                    new BitmapImage(new Uri(игра.Обложка));
            }
            else
            {
                Обложка.Source = null;
            }
        }

        // =========================================================
        // ДОБАВИТЬ НОВУЮ ИГРУ
        // =========================================================

        private void Добавить_Click(object sender, RoutedEventArgs e)
        {
            текущая = new Игра();

            Название.Clear();
            Категория.Text = "Игры";
            Описание.Clear();
            Путь.Clear();

            Скрыта.IsChecked = false;

            Обложка.Source = null;
        }

        // =========================================================
        // ВЫБОР ОБЛОЖКИ
        // =========================================================

        private void Обложка_Click(object sender, RoutedEventArgs e)
        {
            if (текущая == null)
                return;

            var диалог = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.jpg;*.jpeg;*.webp"
            };

            if (диалог.ShowDialog() != true)
                return;

            текущая.Обложка =
                сервис.СкопироватьОбложку(
                    idПК,
                    диалог.FileName);

            Обложка.Source =
                new BitmapImage(new Uri(текущая.Обложка));
        }

        // =========================================================
        // ВЫБОР EXE
        // =========================================================

        private void Путь_Click(object sender, RoutedEventArgs e)
        {
            var диалог = new OpenFileDialog
            {
                Filter = "Исполняемые файлы|*.exe"
            };

            if (диалог.ShowDialog() == true)
            {
                Путь.Text = диалог.FileName;
            }
        }

        // =========================================================
        // СОХРАНЕНИЕ
        // =========================================================

        private void Сохранить_Click(object sender, RoutedEventArgs e)
        {
            if (текущая == null)
                текущая = new Игра();

            текущая.Название = Название.Text.Trim();
            текущая.Категория = Категория.Text.Trim();
            текущая.Описание = Описание.Text.Trim();
            текущая.Путь = Путь.Text.Trim();
            текущая.Скрыта = Скрыта.IsChecked == true;

            if (string.IsNullOrWhiteSpace(текущая.Название))
            {
                MessageBox.Show(
                    "Введите название игры.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var список =
                сервис.ПолучитьИгры(idПК);

            if (список.Exists(x => x.Id == текущая.Id))
            {
                сервис.Изменить(idПК, текущая);
            }
            else
            {
                сервис.Добавить(idПК, текущая);

                LiveGameSync.Notify(idПК);
            }

            ПостроитьКарточки();

            TransferManager.CreateJob(
    idПК,
    текущая.Id.ToString(),
    текущая.Название,
    текущая.Путь,
    текущая.Обложка);
        }

        // =========================================================
        // УДАЛЕНИЕ
        // =========================================================

        private void Удалить_Click(object sender, RoutedEventArgs e)
        {
            if (текущая == null)
                return;

            var ответ =
                MessageBox.Show(
                    $"Удалить игру «{текущая.Название}»?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (ответ != MessageBoxResult.Yes)
                return;

            сервис.Удалить(
                idПК,
                текущая.Id);

            текущая = null;

            Добавить_Click(sender, e);

            ПостроитьКарточки();
        }

        private void OnExeDropped(
    string exe)
        {
            текущая =
                AutoCardCreator.Create(exe);

            Название.Text =
                текущая.Название;

            Категория.Text =
                текущая.Категория;

            Путь.Text =
                текущая.Путь;

            Описание.Text = "";
        }

        private void OnCoverDropped(
    string file)
        {
            var окно =
                new ОкноРедактораОбложки(file);

            окно.Owner = this;

            if (окно.ShowDialog() != true)
                return;

            var destination =
                сервис.СкопироватьОбложку(
                    idПК,
                    file);

            текущая!.Обложка =
                destination;

            Обложка.Source =
                new BitmapImage(
                    new Uri(destination));
        }
    }
}