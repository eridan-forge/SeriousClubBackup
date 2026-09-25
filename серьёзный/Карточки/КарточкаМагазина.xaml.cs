using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using серьёзный.Core.CoreShop;

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

        Купить.Click += (_, _) => BuyRequested?.Invoke(Item);
    }
}