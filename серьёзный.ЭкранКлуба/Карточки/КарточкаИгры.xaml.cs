using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using серьёзный.CrystalUI.Animations;
using серьёзный.Модели;
using серьёзный.ЭкранКлуба.Анимации;
using серьёзный.Карточки;

namespace серьёзный.ЭкранКлуба.Карточки;

public partial class КарточкаИгры : UserControl
{
    private Игра? игра;
    private bool избранное;

    public event Action<Игра>? ИграЗапущена;
    public event Action<Игра>? ИзбранноеИзменилось;

    public КарточкаИгры()
    {
        InitializeComponent();

        серьёзный.ЭкранКлуба.Анимации.HoverAnimation.Attach(Корень);

        Играть.Click += (_, _) =>
        {
            if (игра == null)
                return;

            LaunchAnimation.Play(Корень);

            ИграЗапущена?.Invoke(игра);
        };

        КнопкаИзбранное.Click += (_, _) =>
        {
            if (игра == null)
                return;

            избранное = !избранное;

            КнопкаИзбранное.Content =
                избранное ? "★" : "☆";

            ИзбранноеИзменилось?.Invoke(игра);
        };
    }

    public void Загрузить(Игра model, bool isFavorite)
    {
        игра = model;
        избранное = isFavorite;

        Название.Text = model.Название;
        Категория.Text = model.Категория;

        КнопкаИзбранное.Content =
            isFavorite ? "★" : "☆";

        if (File.Exists(model.Обложка))
        {
            Обложка.Source =
                new BitmapImage(new Uri(model.Обложка));
        }
        else
        {
            Обложка.Source = null;
        }
    }
}