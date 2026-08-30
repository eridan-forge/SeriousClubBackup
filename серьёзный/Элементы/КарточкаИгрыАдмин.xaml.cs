using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using серьёзный.Модели;

namespace серьёзный.Элементы
{
    public partial class КарточкаИгрыАдмин : UserControl
    {
        public Игра? Игра { get; private set; }

        public event Action<Игра>? Редактировать;

        public КарточкаИгрыАдмин()
        {
            InitializeComponent();
        }

        public void Загрузить(Игра игра)
        {
            Игра = игра;

            Название.Text = игра.Название;
            Категория.Text = игра.Категория;

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

        private void Редактировать_Click(object sender, RoutedEventArgs e)
        {
            if (Игра != null)
                Редактировать?.Invoke(Игра);
        }
    }
}