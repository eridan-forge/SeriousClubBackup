using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using серьёзный.Модели;

namespace серьёзный.Окна
{
    public partial class ОкноЗапуска : Window
    {
        private readonly Игра game;

        public ОкноЗапуска(Игра game)
        {
            InitializeComponent();

            this.game = game;

            Loaded += Start;
        }

        private async void Start(
            object sender,
            RoutedEventArgs e)
        {
            Title.Text =
                game.Название;

            if (System.IO.File.Exists(game.Обложка))
            {
                Cover.Source =
                    new BitmapImage(
                        new Uri(game.Обложка));
            }

            await Task.Delay(1400);

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = game.Путь,
                    UseShellExecute = true
                });

            Close();
        }
    }
}