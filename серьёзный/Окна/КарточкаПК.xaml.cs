using System.Windows.Controls;
using System.Windows.Media;

namespace серьёзный
{
    public partial class КарточкаПК : UserControl
    {
        public int КомпьютерId { get; set; }

        public КарточкаПК()
        {
            InitializeComponent();
        }

        public void Обновить(
            string название,
            string время,
            string статус)
        {
            Название.Text = название;
            Таймер.Text = время;
            Статус.Text = статус;

            switch (статус)
            {
                case "Активен":
                    Карточка.Background =
                        new SolidColorBrush(
                            Color.FromRgb(22, 101, 52));
                    break;

                case "Выключен":
                    Карточка.Background =
                        new SolidColorBrush(
                            Color.FromRgb(127, 29, 29));
                    break;

                case "Пауза":
                    Карточка.Background =
                        new SolidColorBrush(
                            Color.FromRgb(120, 53, 15));
                    break;

                default:
                    Карточка.Background =
                        new SolidColorBrush(
                            Color.FromRgb(31, 41, 55));
                    break;
            }
        }
    }
}