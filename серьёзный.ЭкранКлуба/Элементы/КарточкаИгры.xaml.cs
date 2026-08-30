using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using серьёзный.Модели;

namespace серьёзный.ЭкранКлуба.Элементы
{
    public partial class КарточкаИгры : UserControl
    {
        public Игра? Игра { get; private set; }

        public bool Избранная { get; private set; }

        public event Action<Игра>? ИграЗапущена;
        public event Action<Игра>? ИзбранноеИзменилось;

        public КарточкаИгры()
        {
            InitializeComponent();
        }

        public void Загрузить(Игра игра, bool избранная)
        {
            Игра = игра;
            Избранная = избранная;

            Название.Text = игра.Название;
            Категория.Text = игра.Категория;

            Избранное.Content = избранная ? "★" : "☆";

            if (File.Exists(игра.Обложка))
            {
                Обложка.Source =
                    new BitmapImage(new Uri(игра.Обложка));
            }
            else
            {
                Обложка.Source = null;
            }
        }

        private void Избранное_Click(object sender, RoutedEventArgs e)
        {
            if (Игра == null)
                return;

            Избранная = !Избранная;

            Избранное.Content = Избранная ? "★" : "☆";

            ИзбранноеИзменилось?.Invoke(Игра);
        }

        private void Играть_Click(object sender, RoutedEventArgs e)
        {
            if (Игра == null)
                return;

            try
            {
                if (Игра.Путь.StartsWith("steam://",
                        StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Игра.Путь,
                        UseShellExecute = true
                    });

                    ИграЗапущена?.Invoke(Игра);
                    return;
                }

                if (!File.Exists(Игра.Путь))
                {
                    MessageBox.Show("Файл игры не найден.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = Игра.Путь,
                    WorkingDirectory =
                        Path.GetDirectoryName(Игра.Путь),
                    UseShellExecute = true
                });

                ИграЗапущена?.Invoke(Игра);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}