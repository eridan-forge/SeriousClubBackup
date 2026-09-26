using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using серьёзный.Core.CoreShop;
using System.Windows;

namespace серьёзный.Карточки;

public partial class КарточкаМагазина : UserControl
{
    public ShopItem Item { get; }

    public event Action<ShopItem>? BuyRequested;

    public КарточкаМагазина(ShopItem item)
    {
        InitializeComponent();

        Item = item;

        Название.Text = item.Name;
        Описание.Text = item.Description;
        Цена.Text = $"{item.Price:0} ₽";

        if (!string.IsNullOrWhiteSpace(item.Image) &&
            File.Exists(item.Image))
        {
            Фото.Source = new BitmapImage(new Uri(item.Image));
        }

        if (item.Featured)
                  {
            Бейдж.Background = new SolidColorBrush(Color.FromRgb(0xEA, 0xB3, 0x08));
            ТекстБейджа.Text = "🔥 ХИТ";
            Бейдж.Visibility = Visibility.Visible;
                    }
               else if (item.IsNew)
                    {
            Бейдж.Background = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
            ТекстБейджа.Text = "🆕 НОВИНКА";
            Бейдж.Visibility = Visibility.Visible;
                   }

        Купить.Click += (_, _) => BuyRequested?.Invoke(Item);
    }
}